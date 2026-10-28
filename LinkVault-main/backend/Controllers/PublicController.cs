using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(IResourceService resourceService, ILogger<PublicController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>Get a user's public profile by username or custom slug.</summary>
    [HttpGet("profile/{slug}")]
    public async Task<ActionResult> GetPublicProfile(string slug)
    {
        var profile = await _resourceService.GetPublicProfileAsync(slug);
        
        if (profile == null)
        {
            return NotFound(new { error = "Profile not found" });
        }
        
        return Ok(profile);
    }

    /// <summary>Track a link click from the public profile page.</summary>
    [HttpPost("profile/{slug}/click/{linkId}")]
    public async Task<ActionResult> TrackPublicLinkClick(string slug, Guid linkId)
    {
        await _resourceService.IncrementLinkClickAsync(linkId);
        _logger.LogInformation("Public link click: {LinkId} on profile {Slug}", linkId, slug);
        return Ok();
    }

    /// <summary>Verify a code-locked link on a public profile.</summary>
    [HttpPost("profile/{slug}/unlock/{linkId}")]
    public async Task<ActionResult> UnlockPublicLink(string slug, Guid linkId, UnlockLinkRequestDto request)
    {
        var isValid = await _resourceService.VerifyLinkCodeAsync(linkId, request.Code);
        if (!isValid)
        {
            return BadRequest(new { error = "Invalid code.", valid = false });
        }
        var link = await _resourceService.GetLinkAsync(linkId);
        return Ok(new { url = link?.Url, valid = true });
    }

    // ─── Authenticated Profile Management ────────────────────────────────

    /// <summary>Get the current user's profile theme.</summary>
    [HttpGet("theme")]
    [Authorize]
    public async Task<ActionResult> GetMyTheme()
    {
        var userId = GetUserId();
        var theme = await _resourceService.GetProfileThemeAsync(userId);
        return Ok(theme); // null is fine — frontend uses defaults
    }

    /// <summary>Create or update the current user's profile theme.</summary>
    [HttpPut("theme")]
    [Authorize]
    public async Task<ActionResult> UpdateMyTheme(UpdateProfileThemeDto request)
    {
        var userId = GetUserId();
        var theme = await _resourceService.UpsertProfileThemeAsync(userId, request);
        return Ok(theme);
    }

    /// <summary>Update the current user's social links.</summary>
    [HttpPut("social-links")]
    [Authorize]
    public async Task<ActionResult> UpdateSocialLinks([FromBody] Dictionary<string, string> socialLinks)
    {
        var userId = GetUserId();
        await _resourceService.UpdateSocialLinksAsync(userId, socialLinks);
        return Ok(new { message = "Social links updated." });
    }

    /// <summary>Update the current user's public profile URL slug.</summary>
    [HttpPut("slug")]
    [Authorize]
    public async Task<ActionResult> UpdatePublicSlug([FromBody] UpdateSlugDto request)
    {
        var userId = GetUserId();
        var success = await _resourceService.UpdatePublicSlugAsync(userId, request.Slug);
        if (!success)
        {
            return Conflict(new { error = "That slug is already taken." });
        }
        return Ok(new { message = "Slug updated.", slug = request.Slug });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid user claim.");
        return userId;
    }
}

public class UpdateSlugDto
{
    public string Slug { get; set; } = string.Empty;
}
