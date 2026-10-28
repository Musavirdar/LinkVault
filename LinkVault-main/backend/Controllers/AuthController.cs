using LinkVaultApi.Dtos;
using LinkVaultApi.Middleware;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [SkipAudit]
    public async Task<ActionResult<RegisterResponseDto>> Register(RegisterRequestDto request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [SkipAudit]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("2fa/verify")]
    [EnableRateLimiting("auth")]
    [SkipAudit]
    public async Task<ActionResult<LoginResponseDto>> VerifyTwoFactor(TwoFactorVerifyRequestDto request)
    {
        try
        {
            var response = await _authService.VerifyTwoFactorAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupResponseDto>> SetupTwoFactor()
    {
        var userId = GetCurrentUserId();
        var response = await _authService.SetupTwoFactorAsync(userId);
        return Ok(response);
    }

    [HttpPost("2fa/setup/verify")]
    [Authorize]
    public async Task<ActionResult> VerifyTwoFactorSetup(TwoFactorSetupVerifyDto request)
    {
        var userId = GetCurrentUserId();
        var success = await _authService.VerifyTwoFactorSetupAsync(userId, request);
        
        if (!success)
        {
            return BadRequest(new { error = "Invalid verification code" });
        }
        
        return Ok(new { message = "Two-factor authentication enabled successfully" });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ActionResult> DisableTwoFactor([FromBody] string code)
    {
        var userId = GetCurrentUserId();
        var success = await _authService.DisableTwoFactorAsync(userId, code);
        
        if (!success)
        {
            return BadRequest(new { error = "Invalid verification code" });
        }
        
        return Ok(new { message = "Two-factor authentication disabled successfully" });
    }

    [HttpPost("refresh")]
    [SkipAudit]
    public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        await _authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        var profile = await _authService.GetProfileAsync(userId);
        
        if (profile == null)
        {
            return NotFound();
        }
        
        return Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(UpdateProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        var profile = await _authService.UpdateProfileAsync(userId, request);
        
        if (profile == null)
        {
            return NotFound();
        }
        
        return Ok(profile);
    }

    [HttpPost("change-password")]
    [EnableRateLimiting("auth")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequestDto request)
    {
        var userId = GetCurrentUserId();
        var success = await _authService.ChangePasswordAsync(userId, request);
        
        if (!success)
        {
            return BadRequest(new { error = "Current password is incorrect" });
        }
        
        return Ok(new { message = "Password changed successfully" });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }
}
