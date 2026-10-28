using LinkVaultApi.Dtos;
using LinkVaultApi.Models;

namespace LinkVaultApi.Services;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> LoginWithOAuthAsync(string provider, string providerId, string email, string name, string? avatarUrl);
    Task<LoginResponseDto> VerifyTwoFactorAsync(TwoFactorVerifyRequestDto request);
    Task<TwoFactorSetupResponseDto> SetupTwoFactorAsync(Guid userId);
    Task<bool> VerifyTwoFactorSetupAsync(Guid userId, TwoFactorSetupVerifyDto request);
    Task<bool> DisableTwoFactorAsync(Guid userId, string code);
    Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> RevokeRefreshTokenAsync(Guid userId, string refreshToken);
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request);
    Task<bool> LogoutAsync(Guid userId);
}
