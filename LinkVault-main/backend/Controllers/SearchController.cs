using LinkVaultApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly LinkVaultDbContext _db;

    public SearchController(LinkVaultDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Cross-entity search across Links, Resources, and Directories.
    /// Returns up to 5 results per entity type.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters." });

        var term = q.Trim().ToLower();
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var orgIdClaim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(orgIdClaim, out var orgId))
            return Unauthorized();


        var links = await _db.Links
            .Where(l => l.UserId == userId &&
                        (l.Title.ToLower().Contains(term) || (l.Description != null && l.Description.ToLower().Contains(term)) || l.Url.ToLower().Contains(term)))
            .Take(5)
            .Select(l => new { type = "link", id = l.Id, title = l.Title, subtitle = l.Url, icon = "link" })
            .ToListAsync();

        var resources = await _db.Resources
            .Where(r => r.OrganizationId == orgId && r.IsActive &&
                        (r.Name.ToLower().Contains(term) || (r.Description != null && r.Description.ToLower().Contains(term))))
            .Take(5)
            .Select(r => new { type = "resource", id = r.Id, title = r.Name, subtitle = r.MimeType ?? "file", icon = "file" })
            .ToListAsync();

        var directories = await _db.Directories
            .Where(d => d.OrganizationId == orgId &&
                        (d.Name.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term))))
            .Take(5)
            .Select(d => new { type = "directory", id = d.Id, title = d.Name, subtitle = d.Description ?? "", icon = "folder" })
            .ToListAsync();

        return Ok(new
        {
            query = q,
            totalResults = links.Count + resources.Count + directories.Count,
            results = new
            {
                links,
                resources,
                directories
            }
        });
    }
}
