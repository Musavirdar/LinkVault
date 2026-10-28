using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LinksController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<LinksController> _logger;

    public LinksController(IResourceService resourceService, ILogger<LinksController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<LinkDto>>> GetLinks()
    {
        var userId = GetCurrentUserId();
        var links = await _resourceService.GetLinksAsync(userId);
        return Ok(links);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LinkDto>> GetLink(Guid id)
    {
        var link = await _resourceService.GetLinkAsync(id);
        
        if (link == null)
        {
            return NotFound();
        }
        
        return Ok(link);
    }

    [HttpPost]
    [Authorize(Policy = "CanUpload")]
    public async Task<ActionResult<LinkDto>> CreateLink(CreateLinkRequestDto request)
    {
        var userId = GetCurrentUserId();
        var orgId = GetCurrentOrganizationId();
        
        var link = await _resourceService.CreateLinkAsync(userId, orgId, request);
        
        _logger.LogInformation("User {UserId} created link {LinkId}", userId, link.Id);
        
        return CreatedAtAction(nameof(GetLink), new { id = link.Id }, link);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdate")]
    public async Task<ActionResult<LinkDto>> UpdateLink(Guid id, UpdateLinkRequestDto request)
    {
        var userId = GetCurrentUserId();
        
        var link = await _resourceService.UpdateLinkAsync(id, userId, request);
        
        if (link == null)
        {
            return NotFound();
        }
        
        _logger.LogInformation("User {UserId} updated link {LinkId}", userId, id);
        
        return Ok(link);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDelete")]
    public async Task<ActionResult> DeleteLink(Guid id)
    {
        var userId = GetCurrentUserId();
        
        var success = await _resourceService.DeleteLinkAsync(id, userId);
        
        if (!success)
        {
            return NotFound();
        }
        
        _logger.LogInformation("User {UserId} deleted link {LinkId}", userId, id);
        
        return NoContent();
    }

    [HttpPost("{id}/click")]
    [AllowAnonymous]
    public async Task<ActionResult> TrackClick(Guid id)
    {
        await _resourceService.IncrementLinkClickAsync(id);
        return Ok();
    }

    /// <summary>Bulk reorder links by providing an ordered list of link IDs.</summary>
    [HttpPut("reorder")]
    public async Task<ActionResult> ReorderLinks(ReorderLinksRequestDto request)
    {
        var userId = GetCurrentUserId();
        await _resourceService.ReorderLinksAsync(userId, request.LinkIds);
        return Ok(new { message = "Links reordered successfully." });
    }

    /// <summary>Verify a code-locked link's 4-digit PIN.</summary>
    [HttpPost("{id}/unlock")]
    [AllowAnonymous]
    public async Task<ActionResult> UnlockLink(Guid id, UnlockLinkRequestDto request)
    {
        var isValid = await _resourceService.VerifyLinkCodeAsync(id, request.Code);
        if (!isValid)
        {
            return BadRequest(new { error = "Invalid code." });
        }
        // Return the link's URL so the client can redirect
        var link = await _resourceService.GetLinkAsync(id);
        return Ok(new { url = link?.Url, valid = true });
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

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirst("orgId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
        {
            throw new UnauthorizedAccessException("Invalid organization ID");
        }
        return orgId;
    }
}
