namespace LinkVault.Api.Models.DTOs.Auth;

/// <summary>Returned on first password-login when 2FA is enabled but not yet confirmed.</summary>
public class TwoFactorChallengeResponse
{
    /// <summary>Short-lived token (10 min) exchanged for a real JWT once TOTP is verified.</summary>
    public string TwoFactorToken { get; set; } = string.Empty;
    public bool Require2FA { get; set; } = true;
}

public class TwoFactorSetupResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
}

public class TwoFactorVerifyRequest
{
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorLoginRequest
{
    /// <summary>Short-lived 2FA challenge token issued after successful password check.</summary>
    public string TwoFactorToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
