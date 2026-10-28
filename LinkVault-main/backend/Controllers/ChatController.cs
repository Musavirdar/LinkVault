using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly LinkVaultDbContext _db;

    public ChatController(LinkVaultDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the org-level global chat history (messages not bound to a specific directory).
    /// Falls back to returning the 50 most-recent org messages regardless of directory.
    /// </summary>
    [HttpGet("org/{orgId}/history")]
    public async Task<IActionResult> GetOrgHistory(string orgId, [FromQuery] int limit = 50)
    {
        var callerOrgId = User.FindFirst("orgId")?.Value;
        if (callerOrgId == null || callerOrgId != orgId)
            return Forbid();

        if (!Guid.TryParse(orgId, out var orgGuid))
            return BadRequest(new { error = "Invalid org ID" });

        var messages = await _db.ChatMessages
            .Where(m => m.OrganizationId == orgGuid && !m.IsDeleted && m.DirectoryId == null)
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();

        var result = messages.Select(m => new
        {
            id = m.Id.ToString(),
            content = m.Content,
            senderName = m.User?.DisplayName ?? m.User?.Username ?? "Unknown",
            senderAvatarUrl = m.User?.AvatarUrl,
            createdAt = m.CreatedAt.ToString("o"),
            isEdited = m.IsEdited
        });

        return Ok(result);
    }
}
