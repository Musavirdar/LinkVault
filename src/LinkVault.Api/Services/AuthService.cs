using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Auth;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LinkVault.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ITwoFactorService _twoFactor;
    private readonly ITokenService _tokenService;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ITwoFactorService twoFactor,
        ITokenService tokenService)
    {
        _context = context;
        _configuration = configuration;
        _twoFactor = twoFactor;
        _tokenService = tokenService;
    }

    // ── Register ──────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException("Email already registered");

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new ConflictException("Username already taken");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            AccountType = AccountType.Individual,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await BuildAuthResponseAsync(user);
    }

    // ── Login ─────────────────────────────────────────────────────────────
    // Returns AuthResponse for users without 2FA
    // Returns TwoFactorChallengeResponse for users with 2FA enabled

    public async Task<object> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is disabled");

        // If 2FA is fully enrolled, issue a short-lived challenge token
        if (user.TwoFactorEnabled && user.Is2FASetupComplete)
        {
            var challengeToken = _tokenService.Generate2FAToken(user.Id);
            return new TwoFactorChallengeResponse { TwoFactorToken = challengeToken };
        }

        return await BuildAuthResponseAsync(user);
    }

    // ── 2FA: Setup ────────────────────────────────────────────────────────

    public async Task<TwoFactorSetupResponse> Setup2FAAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        if (user.Is2FASetupComplete)
            throw new ConflictException("2FA is already enabled. Disable it first.");

        var (secret, qrCodeUri) = _twoFactor.GenerateSetup(user.Email);

        // Store the secret but don't mark as complete yet — user must verify first
        user.TwoFactorSecret = secret;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new TwoFactorSetupResponse { Secret = secret, QrCodeUri = qrCodeUri };
    }

    // ── 2FA: Verify Setup ─────────────────────────────────────────────────

    public async Task<AuthResponse> VerifySetup2FAAsync(Guid userId, string code)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            throw new ConflictException("2FA setup not started. Call /2fa/setup first.");

        if (!_twoFactor.ValidateCode(user.TwoFactorSecret, code))
            throw new UnauthorizedException("Invalid 2FA code");

        user.TwoFactorEnabled = true;
        user.Is2FASetupComplete = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await BuildAuthResponseAsync(user);
    }

    // ── 2FA: Login Step 2 ─────────────────────────────────────────────────

    public async Task<AuthResponse> LoginWith2FAAsync(string twoFactorToken, string code)
    {
        var principal = _tokenService.ValidateToken(twoFactorToken)
            ?? throw new UnauthorizedException("Invalid or expired 2FA token");

        var purpose = principal.FindFirst("purpose")?.Value;
        if (purpose != "2fa")
            throw new UnauthorizedException("Invalid token purpose");

        var userId = Guid.Parse(principal.FindFirst("sub")!.Value);

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User not found");

        if (!_twoFactor.ValidateCode(user.TwoFactorSecret!, code))
            throw new UnauthorizedException("Invalid 2FA code");

        return await BuildAuthResponseAsync(user);
    }

    // ── 2FA: Disable ─────────────────────────────────────────────────────

    public async Task Disable2FAAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        // Corporate users cannot disable 2FA
        if (user.AccountType == AccountType.Corporate)
            throw new ConflictException("2FA is mandatory for corporate accounts and cannot be disabled");

        user.TwoFactorEnabled = false;
        user.Is2FASetupComplete = false;
        user.TwoFactorSecret = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ── Existing Methods ──────────────────────────────────────────────────

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || token.IsRevoked || token.IsExpired)
            throw new UnauthorizedException("Invalid refresh token");

        token.IsRevoked = true;
        await _context.SaveChangesAsync();

        return await BuildAuthResponseAsync(token.User);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        return MapToUserDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect");

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = rawRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Username = user.Username,
        FirstName = user.FirstName,
        LastName = user.LastName,
        AvatarUrl = user.AvatarUrl,
        AccountType = user.AccountType.ToString(),
        CreatedAt = user.CreatedAt
    };

    private static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    private static bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
