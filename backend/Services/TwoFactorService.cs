using OtpNet;
using QRCoder;
using System.Security.Cryptography;

namespace LinkVaultApi.Services;

public class TwoFactorService : ITwoFactorService
{
    public string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string secretKey, string username, string issuer)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";
    }

    public string GenerateQrCodeImageBase64(string qrCodeUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        return Convert.ToBase64String(qrCodeImage);
    }

    public bool VerifyCode(string secretKey, string code)
    {
        var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(secretKeyBytes);
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    public string[] GenerateBackupCodes()
    {
        var backupCodes = new string[10];
        using var rng = RandomNumberGenerator.Create();
        
        for (int i = 0; i < 10; i++)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = BitConverter.ToUInt32(bytes, 0) % 100000000;
            backupCodes[i] = code.ToString("D8");
        }
        
        return backupCodes;
    }

    public bool VerifyBackupCode(string[] backupCodes, string code)
    {
        return backupCodes.Contains(code);
    }
}
