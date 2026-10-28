using LinkVaultApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/auth")]
public class OAuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IAuthService authService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{provider}")]
    public IActionResult Login(string provider)
    {
        provider = provider.ToLower();
        var settings = _configuration.GetSection($"SsoSettings:{GetProviderKey(provider)}");
        
        if (string.IsNullOrEmpty(settings["ClientId"]) || settings["ClientId"] == "your-google-client-id")
        {
            return BadRequest(new { error = $"{provider} SSO is not configured on this server." });
        }

        var redirectUri = GetCallbackUri(provider);
        var clientId = settings["ClientId"];
        
        string authUrl = provider switch
        {
            "google" => $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=email profile&access_type=offline&prompt=consent",
            "github" => $"https://github.com/login/oauth/authorize?client_id={clientId}&redirect_uri={redirectUri}&scope=read:user user:email",
            "linkedin" => $"https://www.linkedin.com/oauth/v2/authorization?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=r_liteprofile r_emailaddress",
            _ => null
        };

        if (authUrl == null) return NotFound(new { error = "Provider not supported" });

        return Redirect(authUrl);
    }

    [HttpGet("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return RedirectToFrontendError(error);
        }

        if (string.IsNullOrEmpty(code))
        {
            return RedirectToFrontendError("No code provided");
        }

        provider = provider.ToLower();
        
        try
        {
            var userInfo = await ExchangeCodeForUserInfoAsync(provider, code);
            if (userInfo == null) return RedirectToFrontendError("Failed to retrieve user info");

            var loginResult = await _authService.LoginWithOAuthAsync(
                provider, 
                userInfo.Id, 
                userInfo.Email, 
                userInfo.Name, 
                userInfo.AvatarUrl);

            var frontendUrl = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:5173";
            
            // Redirect to frontend with tokens in URL hash (standard OAuth frontend handoff)
            return Redirect($"{frontendUrl}/oauth-callback#token={loginResult.Token}&refreshToken={loginResult.RefreshToken}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed for {Provider}", provider);
            return RedirectToFrontendError("Authentication failed");
        }
    }

    private IActionResult RedirectToFrontendError(string errorMessage)
    {
        var frontendUrl = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:5173";
        return Redirect($"{frontendUrl}/login?error={HttpUtility.UrlEncode(errorMessage)}");
    }

    private string GetCallbackUri(string provider)
    {
        // In production, this should read from configuration BaseUrl
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return $"{baseUrl}/api/auth/callback/{provider}";
    }

    private string GetProviderKey(string provider) => provider switch
    {
        "google" => "Google",
        "github" => "GitHub",
        "linkedin" => "LinkedIn",
        _ => throw new ArgumentException("Invalid provider")
    };

    private async Task<OAuthUserInfo?> ExchangeCodeForUserInfoAsync(string provider, string code)
    {
        var settings = _configuration.GetSection($"SsoSettings:{GetProviderKey(provider)}");
        var clientId = settings["ClientId"]!;
        var clientSecret = settings["ClientSecret"]!;
        var redirectUri = GetCallbackUri(provider);

        using var client = _httpClientFactory.CreateClient();

        return provider switch
        {
            "google" => await HandleGoogleAsync(client, code, clientId, clientSecret, redirectUri),
            "github" => await HandleGitHubAsync(client, code, clientId, clientSecret, redirectUri),
            "linkedin" => await HandleLinkedInAsync(client, code, clientId, clientSecret, redirectUri),
            _ => null
        };
    }

    private async Task<OAuthUserInfo?> HandleGoogleAsync(HttpClient client, string code, string clientId, string clientSecret, string redirectUri)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        });

        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenData.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        if (!userResponse.IsSuccessStatusCode) return null;

        var userData = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        return new OAuthUserInfo
        {
            Id = userData.GetProperty("id").GetString(),
            Email = userData.GetProperty("email").GetString(),
            Name = userData.GetProperty("name").GetString(),
            AvatarUrl = userData.TryGetProperty("picture", out var pic) ? pic.GetString() : null
        };
    }

    private async Task<OAuthUserInfo?> HandleGitHubAsync(HttpClient client, string code, string clientId, string clientSecret, string redirectUri)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var tokenResponse = await client.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        if (!tokenData.TryGetProperty("access_token", out var accessTokenProp)) return null;
        var accessToken = accessTokenProp.GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LinkVault", "1.0"));
        
        var userResponse = await client.GetAsync("https://api.github.com/user");
        if (!userResponse.IsSuccessStatusCode) return null;

        var userData = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        // GitHub emails are often fetched separately if private
        string email = userData.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null;
        
        if (string.IsNullOrEmpty(email))
        {
            var emailsResponse = await client.GetAsync("https://api.github.com/user/emails");
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emailsData = await emailsResponse.Content.ReadFromJsonAsync<JsonElement[]>();
                email = emailsData?.FirstOrDefault(x => x.GetProperty("primary").GetBoolean()).GetProperty("email").GetString();
            }
        }

        if (string.IsNullOrEmpty(email)) return null; // Email is required for our system

        return new OAuthUserInfo
        {
            Id = userData.GetProperty("id").GetInt64().ToString(),
            Email = email,
            Name = userData.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null ? n.GetString() : userData.GetProperty("login").GetString(),
            AvatarUrl = userData.TryGetProperty("avatar_url", out var pic) ? pic.GetString() : null
        };
    }

    private async Task<OAuthUserInfo?> HandleLinkedInAsync(HttpClient client, string code, string clientId, string clientSecret, string redirectUri)
    {
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        var tokenResponse = await client.PostAsync("https://www.linkedin.com/oauth/v2/accessToken", tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode) return null;

        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenData.GetProperty("access_token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        // LinkedIn v2 API requires fetching profile and email separately
        var profileResponse = await client.GetAsync("https://api.linkedin.com/v2/me?projection=(id,localizedFirstName,localizedLastName,profilePicture(displayImage~:playableStreams))");
        var emailResponse = await client.GetAsync("https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))");
        
        if (!profileResponse.IsSuccessStatusCode || !emailResponse.IsSuccessStatusCode) return null;

        var profileData = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var emailData = await emailResponse.Content.ReadFromJsonAsync<JsonElement>();

        var firstName = profileData.GetProperty("localizedFirstName").GetString();
        var lastName = profileData.GetProperty("localizedLastName").GetString();
        
        string avatarUrl = null;
        try {
            avatarUrl = profileData.GetProperty("profilePicture").GetProperty("displayImage~").GetProperty("elements")[0].GetProperty("identifiers")[0].GetProperty("identifier").GetString();
        } catch { }

        var email = emailData.GetProperty("elements")[0].GetProperty("handle~").GetProperty("emailAddress").GetString();

        return new OAuthUserInfo
        {
            Id = profileData.GetProperty("id").GetString(),
            Email = email,
            Name = $"{firstName} {lastName}".Trim(),
            AvatarUrl = avatarUrl
        };
    }

    private class OAuthUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
