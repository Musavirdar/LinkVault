using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinkVaultApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var orgId = Context.User?.FindFirst("orgId")?.Value;

        _logger.LogInformation("User {UserId} ({UserName}) connected to chat", userId, userName);

        // Auto-join the org-level global chat channel on connect
        if (orgId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org:{orgId}");
        }

        await base.OnConnectedAsync();
    }


    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var orgId = Context.User?.FindFirst("orgId")?.Value;

        if (orgId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org:{orgId}");
        }

        _logger.LogInformation("User {UserId} ({UserName}) disconnected from chat", userId, userName);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Broadcast a text message to all org members in the global channel.</summary>
    public async Task SendOrgMessage(string orgId, string content)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst("unique_name")?.Value
                       ?? Context.User?.FindFirst(ClaimTypes.Name)?.Value
                       ?? "Unknown";

        if (string.IsNullOrWhiteSpace(content)) return;

        var msg = new
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            SenderName = userName,
            SentAt = DateTime.UtcNow.ToString("o")
        };

        await Clients.Group($"org:{orgId}").SendAsync("OrgMessage", msg);
    }



    public async Task JoinDirectory(string directoryId)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        
        await Groups.AddToGroupAsync(Context.ConnectionId, directoryId);
        
        _logger.LogInformation("User {UserId} ({UserName}) joined directory {DirectoryId}", userId, userName, directoryId);
        
        // Notify others that user joined
        await Clients.Group(directoryId).SendAsync("UserJoined", new
        {
            UserId = userId,
            UserName = userName,
            DirectoryId = directoryId,
            JoinedAt = DateTime.UtcNow
        });
    }

    public async Task LeaveDirectory(string directoryId)
    {
        var userId = GetUserId();
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, directoryId);
        
        _logger.LogInformation("User {UserId} left directory {DirectoryId}", userId, directoryId);
        
        // Notify others that user left
        await Clients.Group(directoryId).SendAsync("UserLeft", new
        {
            UserId = userId,
            DirectoryId = directoryId,
            LeftAt = DateTime.UtcNow
        });
    }

    public async Task SendMessage(string directoryId, SendMessageRequestDto request)
    {
        var userId = GetUserId();
        
        try
        {
            var message = await _chatService.SendMessageAsync(
                Guid.Parse(directoryId), 
                userId, 
                request);

            // Broadcast to all users in the directory
            await Clients.Group(directoryId).SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task EditMessage(string directoryId, string messageId, EditMessageRequestDto request)
    {
        var userId = GetUserId();
        
        try
        {
            var message = await _chatService.EditMessageAsync(
                Guid.Parse(messageId), 
                userId, 
                request);

            if (message != null)
            {
                await Clients.Group(directoryId).SendAsync("MessageEdited", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task DeleteMessage(string directoryId, string messageId)
    {
        var userId = GetUserId();
        
        try
        {
            var success = await _chatService.DeleteMessageAsync(Guid.Parse(messageId), userId);

            if (success)
            {
                await Clients.Group(directoryId).SendAsync("MessageDeleted", new { MessageId = messageId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public async Task Typing(string directoryId, bool isTyping)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        
        await Clients.Group(directoryId).SendAsync("UserTyping", new TypingIndicatorDto
        {
            DirectoryId = Guid.Parse(directoryId),
            UserId = userId,
            UserName = userName,
            IsTyping = isTyping
        });
    }

    public async Task LoadHistory(string directoryId, ChatHistoryRequestDto request)
    {
        try
        {
            var history = await _chatService.GetChatHistoryAsync(Guid.Parse(directoryId), request);
            await Clients.Caller.SendAsync("ChatHistory", history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chat history");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        throw new HubException("User not authenticated");
    }
}
