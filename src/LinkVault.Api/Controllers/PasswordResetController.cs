using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _resetService;

    public PasswordResetController(IPasswordResetService resetService)
    {
        _resetService = resetService;
    }

    /// <summary>
    /// Step 1: Send a password reset link to the given email.
    /// Always returns 200 to avoid email enumeration attacks.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _resetService.GenerateResetTokenAsync(request.Email);
        }
        catch
        {
            // Swallow all errors — never reveal whether an email exists
        }
        return Ok(new { message = "If an account exists for that email, a reset link has been sent." });
    }

    /// <summary>
    /// Step 2: Submit the token from the email + a new password.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _resetService.ResetPasswordAsync(request.Token, request.NewPassword);
        return Ok(new { message = "Password reset successfully. You can now log in." });
    }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
