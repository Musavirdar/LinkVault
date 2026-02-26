using LinkVault.Api.Data;
using LinkVault.Api.Extensions;
using LinkVault.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Controllers;

/// <summary>
/// Chat history retrieval endpoints (real-time is handled by ChatHub at /hubs/chat).
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChatController(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Get the last N messages for a directory chat.
    /// </summary>
    [HttpGet("directory/{directoryId}")]
    public async Task<IActionResult> GetDirectoryMessages(
        Guid directoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.DirectoryId == directoryId && m.ChatType == ChatType.DirectoryChat)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                DirectoryId = m.DirectoryId,
                OrganizationId = m.OrganizationId,
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited
            })
            .ToListAsync();

        // Return in chronological order for display
        messages.Reverse();
        return Ok(messages);
    }

    /// <summary>
    /// Get the last N messages for an organization-wide chat.
    /// </summary>
    [HttpGet("organization/{orgId}")]
    public async Task<IActionResult> GetOrganizationMessages(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Ensure the requesting user is a member
        var userId = User.GetUserId();
        var isMember = await _context.Users
            .AnyAsync(u => u.Id == userId && u.OrganizationId == orgId);

        if (!isMember)
            return Forbid();

        var messages = await _context.ChatMessages
            .Where(m => m.OrganizationId == orgId && m.ChatType == ChatType.OrganizationChat)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                DirectoryId = m.DirectoryId,
                OrganizationId = m.OrganizationId,
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited
            })
            .ToListAsync();

        messages.Reverse();
        return Ok(messages);
    }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid? DirectoryId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsEdited { get; set; }
}
