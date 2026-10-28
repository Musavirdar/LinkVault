using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DirectoriesController : ControllerBase
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<DirectoriesController> _logger;

    public DirectoriesController(IResourceService resourceService, ILogger<DirectoriesController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<DirectoryDto>>> GetDirectories([FromQuery] Guid? parentId)
    {
        var orgId = GetCurrentOrganizationId();
        var cleanParentId = (parentId == Guid.Empty) ? null : parentId;
        var directories = await _resourceService.GetDirectoriesAsync(orgId, cleanParentId);
        return Ok(directories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DirectoryDto>> GetDirectory(Guid id)
    {
        var directory = await _resourceService.GetDirectoryAsync(id);
        
        if (directory == null)
        {
            return NotFound();
        }
        
        // Check organization access
        var orgId = GetCurrentOrganizationId();
        var userOrgId = await GetDirectoryOrganizationIdAsync(id);
        
        if (userOrgId != orgId)
        {
            return Forbid();
        }
        
        return Ok(directory);
    }

    [HttpPost]
    [Authorize(Policy = "CanUpload")]
    public async Task<ActionResult<DirectoryDto>> CreateDirectory(CreateDirectoryRequestDto request)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();

        if (request.ParentDirectoryId == Guid.Empty)
        {
            request.ParentDirectoryId = null;
        }
        
        var directory = await _resourceService.CreateDirectoryAsync(orgId, userId, request);
        
        _logger.LogInformation("User {UserId} created directory {DirectoryId}", userId, directory.Id);
        
        return CreatedAtAction(nameof(GetDirectory), new { id = directory.Id }, directory);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdate")]
    public async Task<ActionResult<DirectoryDto>> UpdateDirectory(Guid id, UpdateDirectoryRequestDto request)
    {
        var userId = GetCurrentUserId();
        
        // Check organization access
        var orgId = GetCurrentOrganizationId();
        var dirOrgId = await GetDirectoryOrganizationIdAsync(id);
        
        if (dirOrgId != orgId)
        {
            return Forbid();
        }
        
        var directory = await _resourceService.UpdateDirectoryAsync(id, userId, request);
        
        if (directory == null)
        {
            return NotFound();
        }
        
        _logger.LogInformation("User {UserId} updated directory {DirectoryId}", userId, id);
        
        return Ok(directory);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDelete")]
    public async Task<ActionResult> DeleteDirectory(Guid id)
    {
        var userId = GetCurrentUserId();
        
        // Check organization access
        var orgId = GetCurrentOrganizationId();
        var dirOrgId = await GetDirectoryOrganizationIdAsync(id);
        
        if (dirOrgId != orgId)
        {
            return Forbid();
        }
        
        var success = await _resourceService.DeleteDirectoryAsync(id, userId);
        
        if (!success)
        {
            return BadRequest(new { error = "Directory not found or contains items" });
        }
        
        _logger.LogInformation("User {UserId} deleted directory {DirectoryId}", userId, id);
        
        return NoContent();
    }

    [HttpGet("{id}/resources")]
    public async Task<ActionResult<List<ResourceDto>>> GetDirectoryResources(Guid id)
    {
        // Check organization access
        var orgId = GetCurrentOrganizationId();
        var dirOrgId = await GetDirectoryOrganizationIdAsync(id);
        
        if (dirOrgId != orgId)
        {
            return Forbid();
        }
        
        var resources = await _resourceService.GetResourcesAsync(id);
        return Ok(resources);
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

    private async Task<Guid> GetDirectoryOrganizationIdAsync(Guid directoryId)
    {
        var directory = await _resourceService.GetDirectoryAsync(directoryId);
        if (directory == null)
        {
            throw new KeyNotFoundException("Directory not found");
        }
        
        return directory.OrganizationId;
    }
}
