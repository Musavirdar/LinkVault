using LinkVault.Api.Models.DTOs.Auth;

namespace LinkVault.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<object> LoginAsync(LoginRequest request);  // Returns AuthResponse or TwoFactorChallengeResponse
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    // 2FA
    Task<TwoFactorSetupResponse> Setup2FAAsync(Guid userId);
    Task<AuthResponse> VerifySetup2FAAsync(Guid userId, string code);
    Task Disable2FAAsync(Guid userId);
    Task<AuthResponse> LoginWith2FAAsync(string twoFactorToken, string code);
}

