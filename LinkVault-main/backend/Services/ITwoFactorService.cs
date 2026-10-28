namespace LinkVaultApi.Services;

public interface ITwoFactorService
{
    string GenerateSecretKey();
    string GenerateQrCodeUri(string secretKey, string username, string issuer);
    string GenerateQrCodeImageBase64(string qrCodeUri);
    bool VerifyCode(string secretKey, string code);
    string[] GenerateBackupCodes();
    bool VerifyBackupCode(string[] backupCodes, string code);
}
