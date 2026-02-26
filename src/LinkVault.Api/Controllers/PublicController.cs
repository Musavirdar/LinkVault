using LinkVault.Api.Data;
using LinkVault.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Controllers;

/// <summary>
/// Unauthenticated public endpoints — user profiles and link click tracking.
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PublicController(ApplicationDbContext context) => _context = context;

    // ── GET /api/public/u/{username} ────────────────────────────────────────
    [HttpGet("u/{username}")]
    public async Task<IActionResult> GetUserProfile(string username)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null) return NotFound();

        var links = await _context.Links
            .AsNoTracking()
            .Where(l => l.UserId == user.Id && l.IsActive)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new PublicLinkDto
            {
                Id          = l.Id,
                Title       = l.Title ?? l.OriginalUrl,
                Url         = l.OriginalUrl,
                Description = l.Description,
                ClickCount  = l.ClickCount,
            })
            .ToListAsync();

        var profile = new PublicProfileDto
        {
            Username    = user.Username,
            DisplayName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim(),
            AvatarUrl   = user.AvatarUrl,
            Links       = links,
        };

        return Ok(profile);
    }

    // ── POST /api/public/links/{id}/click ────────────────────────────────────
    [HttpPost("links/{id:guid}/click")]
    public async Task<IActionResult> RecordClick(Guid id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link is null || !link.IsActive)
            return NotFound();

        link.ClickCount++;
        link.LastClickedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Redirect to the original URL so the browser follows the link
        return Redirect(link.OriginalUrl);
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class PublicProfileDto
{
    public string Username    { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl  { get; set; }
    public string? Website    { get; set; }
    public List<PublicLinkDto> Links { get; set; } = new();
}

public class PublicLinkDto
{
    public Guid    Id          { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string  Url         { get; set; } = string.Empty;   // frontend uses this field name
    public string? Description { get; set; }
    public string? IconUrl     { get; set; }
    public int     ClickCount  { get; set; }
}
