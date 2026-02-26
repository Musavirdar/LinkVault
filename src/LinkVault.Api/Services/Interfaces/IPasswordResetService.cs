namespace LinkVault.Api.Services.Interfaces;

public interface IPasswordResetService
{
    Task<string> GenerateResetTokenAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
}
