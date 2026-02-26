using System.Security.Claims;
using LinkVault.Api.Data;
using LinkVault.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LinkVault.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Directory (Project) Chat ──────────────────────────────────────────

    public async Task JoinDirectoryChat(string directoryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dir_{directoryId}");
    }

    public async Task LeaveDirectoryChat(string directoryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dir_{directoryId}");
    }

    public async Task SendDirectoryMessage(string directoryId, string content)
    {
        var userId = GetUserId();
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Content = content,
            ChatType = ChatType.DirectoryChat,
            SenderId = userId,
            SenderName = username,
            DirectoryId = Guid.Parse(directoryId),
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"dir_{directoryId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.Content,
            message.SenderId,
            message.SenderName,
            message.DirectoryId,
            message.CreatedAt,
            ChatType = "Directory"
        });
    }

    // ── Organization (General) Chat ───────────────────────────────────────

    public async Task JoinOrganizationChat(string organizationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{organizationId}");
    }

    public async Task LeaveOrganizationChat(string organizationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org_{organizationId}");
    }

    public async Task SendOrganizationMessage(string organizationId, string content)
    {
        var userId = GetUserId();
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Content = content,
            ChatType = ChatType.OrganizationChat,
            SenderId = userId,
            SenderName = username,
            OrganizationId = Guid.Parse(organizationId),
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"org_{organizationId}").SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.Content,
            message.SenderId,
            message.SenderName,
            message.OrganizationId,
            message.CreatedAt,
            ChatType = "Organization"
        });
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new HubException("Not authenticated");
        return Guid.Parse(claim);
    }
}
