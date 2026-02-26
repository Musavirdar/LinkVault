namespace LinkVault.Api.Services.Interfaces;

public interface ITwoFactorService
{
    (string Secret, string QrCodeUri) GenerateSetup(string email);
    bool ValidateCode(string secret, string code);
}
