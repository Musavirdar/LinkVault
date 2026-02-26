using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Auth;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Controllers;

/// <summary>
/// SSO via OAuth2 Authorization Code Flow.
/// The client redirects the user to the provider's OAuth login page,
/// then the provider redirects back to /api/sso/{provider}/callback
/// with an auth code. This endpoint exchanges the code → user info → JWT.
///
/// Supported providers: google, github
///
/// Required appsettings:
/// "Sso": {
///   "Google": { "ClientId": "...", "ClientSecret": "..." },
///   "GitHub": { "ClientId": "...", "ClientSecret": "..." }
/// }
/// </summary>
[ApiController]
[Route("api/sso")]
public class SsoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SsoController> _logger;

    private static readonly Dictionary<string, (string AuthUrl, string TokenUrl, string UserUrl)> Providers = new()
    {
        ["google"] = (
            "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token",
            "https://www.googleapis.com/userinfo/v2/me"),
        ["github"] = (
            "https://github.com/login/oauth/authorize",
            "https://github.com/login/oauth/access_token",
            "https://api.github.com/user")
    };

    public SsoController(
        ApplicationDbContext context,
        ITokenService tokenService,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<SsoController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns the OAuth authorization URL to redirect the user to.
    /// Frontend calls this and redirects the user to the returned URL.
    /// </summary>
    [HttpGet("{provider}/authorize")]
    public IActionResult GetAuthorizeUrl(string provider)
    {
        provider = provider.ToLower();
        if (!Providers.ContainsKey(provider))
            return BadRequest(new { error = $"Unsupported provider: {provider}" });

        var clientId = _config[$"Sso:{Capitalize(provider)}:ClientId"]
            ?? throw new InvalidOperationException($"SSO ClientId for {provider} not configured");

        var callbackUrl = BuildCallbackUrl(provider);
        var (authUrl, _, _) = Providers[provider];

        var url = provider switch
        {
            "google" => $"{authUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&response_type=code&scope=email%20profile&access_type=offline",
            "github" => $"{authUrl}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&scope=user:email",
            _ => throw new InvalidOperationException()
        };

        return Ok(new { url });
    }

    /// <summary>
    /// OAuth callback — exchanges authorization code for user info,
    /// then creates or logs in the user, returning a JWT.
    /// </summary>
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest(new { error });

        provider = provider.ToLower();
        if (!Providers.ContainsKey(provider))
            return BadRequest(new { error = $"Unsupported provider: {provider}" });

        try
        {
            var userInfo = await GetProviderUserInfoAsync(provider, code);

            // Find or create user
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.SsoProvider == provider && u.SsoSubject == userInfo.Id);

            if (user == null)
            {
                // Check if email is already registered — link accounts
                user = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                if (user != null)
                {
                    // Link SSO to existing account
                    user.SsoProvider = provider;
                    user.SsoSubject = userInfo.Id;
                }
                else
                {
                    // Create new user via SSO
                    var username = await GenerateUniqueUsername(userInfo.Name ?? userInfo.Email.Split('@')[0]);
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = userInfo.Email,
                        Username = username,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        AvatarUrl = userInfo.AvatarUrl,
                        SsoProvider = provider,
                        SsoSubject = userInfo.Id,
                        AccountType = AccountType.Individual,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                }

                await _context.SaveChangesAsync();
            }

            // Generate JWT — 2FA check: if corporate and 2FA enrolled, issue challenge
            if (user.TwoFactorEnabled && user.Is2FASetupComplete)
            {
                var challengeToken = _tokenService.Generate2FAToken(user.Id);
                return Ok(new { twoFactorToken = challengeToken, require2FA = true });
            }

            // Generate tokens (reuse RefreshToken entity and TokenService)
            var accessToken = _tokenService.GenerateAccessToken(user);
            var rawRefresh = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = rawRefresh,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken,
                refreshToken = rawRefresh,
                expiresAt = DateTime.UtcNow.AddMinutes(60),
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.AvatarUrl,
                    AccountType = user.AccountType.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSO callback error for provider {Provider}", provider);
            return StatusCode(500, new { error = "SSO login failed" });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<SsoUserInfo> GetProviderUserInfoAsync(string provider, string code)
    {
        var clientId = _config[$"Sso:{Capitalize(provider)}:ClientId"]!;
        var clientSecret = _config[$"Sso:{Capitalize(provider)}:ClientSecret"]!;
        var callbackUrl = BuildCallbackUrl(provider);
        var (_, tokenUrl, userUrl) = Providers[provider];

        var http = _httpClientFactory.CreateClient();

        // Exchange code for access token
        var tokenParams = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = callbackUrl,
            ["grant_type"] = "authorization_code"
        });

        var tokenResp = await http.PostAsync(tokenUrl, tokenParams);
        var tokenJson = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, object>>()
            ?? throw new InvalidOperationException("Failed to get access token");

        var accessToken = tokenJson["access_token"]?.ToString()
            ?? throw new InvalidOperationException("No access_token in response");

        // Fetch user info
        var req = new HttpRequestMessage(HttpMethod.Get, userUrl);
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Headers.Add("User-Agent", "LinkVault");
        req.Headers.Add("Accept", "application/json");

        var userResp = await http.SendAsync(req);
        var userData = await userResp.Content.ReadFromJsonAsync<Dictionary<string, object>>()
            ?? throw new InvalidOperationException("Failed to get user info");

        return provider switch
        {
            "google" => new SsoUserInfo
            {
                Id = userData["id"]?.ToString() ?? "",
                Email = userData["email"]?.ToString() ?? "",
                Name = userData.GetValueOrDefault("name")?.ToString(),
                FirstName = userData.GetValueOrDefault("given_name")?.ToString(),
                LastName = userData.GetValueOrDefault("family_name")?.ToString(),
                AvatarUrl = userData.GetValueOrDefault("picture")?.ToString()
            },
            "github" => new SsoUserInfo
            {
                Id = userData["id"]?.ToString() ?? "",
                Email = userData.GetValueOrDefault("email")?.ToString()
                    ?? throw new InvalidOperationException("GitHub email is not public"),
                Name = userData.GetValueOrDefault("name")?.ToString(),
                AvatarUrl = userData.GetValueOrDefault("avatar_url")?.ToString()
            },
            _ => throw new InvalidOperationException()
        };
    }

    private async Task<string> GenerateUniqueUsername(string base_)
    {
        var candidate = base_.Replace(" ", "").ToLower();
        if (!await _context.Users.AnyAsync(u => u.Username == candidate))
            return candidate;

        for (var i = 1; i < 100; i++)
        {
            var attempt = $"{candidate}{i}";
            if (!await _context.Users.AnyAsync(u => u.Username == attempt))
                return attempt;
        }
        return $"{candidate}{Guid.NewGuid().ToString()[..6]}";
    }

    private string BuildCallbackUrl(string provider) =>
        $"{Request.Scheme}://{Request.Host}/api/sso/{provider}/callback";

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}

internal class SsoUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}
