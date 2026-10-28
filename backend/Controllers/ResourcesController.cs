using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(IResourceService resourceService, ILogger<ResourcesController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceDto>> GetResource(Guid id)
    {
        var resource = await _resourceService.GetResourceAsync(id);

        if (resource == null)
        {
            return NotFound();
        }

        // IDOR guard: ensure the resource belongs to the caller's org
        var orgIdClaim = User.FindFirst("orgId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var orgId) || resource.OrganizationId != orgId)
        {
            return NotFound(); // return 404 not 403 to avoid org enumeration
        }

        // Increment view count
        await _resourceService.IncrementViewCountAsync(id);

        return Ok(resource);
    }


    [HttpPost]
    [Authorize(Policy = "CanUpload")]
    public async Task<ActionResult<ResourceDto>> CreateResource(CreateResourceRequestDto request)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();
        
        var resource = await _resourceService.CreateResourceAsync(orgId, userId, request);
        
        _logger.LogInformation("User {UserId} created resource {ResourceId}", userId, resource.Id);
        
        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdate")]
    public async Task<ActionResult<ResourceDto>> UpdateResource(Guid id, UpdateResourceRequestDto request)
    {
        var userId = GetCurrentUserId();
        
        var resource = await _resourceService.UpdateResourceAsync(id, userId, request);
        
        if (resource == null)
        {
            return NotFound();
        }
        
        _logger.LogInformation("User {UserId} updated resource {ResourceId}", userId, id);
        
        return Ok(resource);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDelete")]
    public async Task<ActionResult> DeleteResource(Guid id)
    {
        var userId = GetCurrentUserId();
        
        var success = await _resourceService.DeleteResourceAsync(id, userId);
        
        if (!success)
        {
            return NotFound();
        }
        
        _logger.LogInformation("User {UserId} deleted resource {ResourceId}", userId, id);
        
        return NoContent();
    }

    [HttpPost("{id}/click")]
    [AllowAnonymous]
    public async Task<ActionResult> TrackClick(Guid id)
    {
        await _resourceService.IncrementClickCountAsync(id);
        return Ok();
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
