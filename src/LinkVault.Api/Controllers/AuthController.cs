using LinkVault.Api.Extensions;
using LinkVault.Api.Models.DTOs.Auth;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    // ── Core Auth ─────────────────────────────────────────────────────────

    /// <summary>
    /// Register a new individual account.
    /// Corporate users are created via invitation — see POST /api/invitations/{token}/accept.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetCurrentUser), response);
    }

    /// <summary>
    /// Login. Returns AuthResponse directly, or TwoFactorChallengeResponse
    /// when the account has 2FA enabled (Corporate accounts always have 2FA).
    /// If you receive TwoFactorChallengeResponse, call POST /auth/login/2fa with
    /// the twoFactorToken + your TOTP code to get the real JWT.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Complete 2FA login: exchange a short-lived challenge token + TOTP code for a real JWT.
    /// </summary>
    [HttpPost("login/2fa")]
    public async Task<ActionResult<AuthResponse>> LoginWith2FA([FromBody] TwoFactorLoginRequest request)
    {
        var response = await _authService.LoginWith2FAAsync(request.TwoFactorToken, request.Code);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync(User.GetUserId());
        return Ok(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(User.GetUserId(), request);
        return NoContent();
    }

    // ── 2FA Enrollment ────────────────────────────────────────────────────

    /// <summary>
    /// Step 1: Generate a TOTP secret and QR code URI.
    /// Display the QR code to the user so they can scan it with an authenticator app.
    /// The secret is stored but 2FA is NOT yet active until VerifySetup is called.
    /// </summary>
    [Authorize]
    [HttpGet("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup2FA()
    {
        var response = await _authService.Setup2FAAsync(User.GetUserId());
        return Ok(response);
    }

    /// <summary>
    /// Step 2: Verify the first TOTP code from the authenticator app.
    /// On success, 2FA is fully enabled and a new JWT is returned.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/setup/verify")]
    public async Task<ActionResult<AuthResponse>> VerifySetup2FA([FromBody] TwoFactorVerifyRequest request)
    {
        var response = await _authService.VerifySetup2FAAsync(User.GetUserId(), request.Code);
        return Ok(response);
    }

    /// <summary>
    /// Disable 2FA. Not allowed for Corporate accounts (2FA is mandatory).
    /// </summary>
    [Authorize]
    [HttpDelete("2fa")]
    public async Task<IActionResult> Disable2FA()
    {
        await _authService.Disable2FAAsync(User.GetUserId());
        return NoContent();
    }
}
