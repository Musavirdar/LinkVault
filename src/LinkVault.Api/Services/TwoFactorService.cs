using LinkVault.Api.Services.Interfaces;
using OtpNet;

namespace LinkVault.Api.Services;

public class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "LinkVault";

    public (string Secret, string QrCodeUri) GenerateSetup(string email)
    {
        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var uri = new OtpUri(OtpType.Totp, secret, email, Issuer).ToString();
        return (secret, uri);
    }

    public bool ValidateCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(2, 2));
    }
}
