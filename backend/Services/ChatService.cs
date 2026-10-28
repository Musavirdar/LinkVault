using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Services;

public class ChatService : IChatService
{
    private readonly LinkVaultDbContext _context;

    public ChatService(LinkVaultDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid directoryId, Guid userId, SendMessageRequestDto request)
    {
        var directory = await _context.Directories.FindAsync(directoryId);
        if (directory == null)
        {
            throw new InvalidOperationException("Directory not found");
        }

        var message = new ChatMessage
        {
            Type = request.Type,
            Content = request.Content,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentName = request.AttachmentName,
            AttachmentSize = request.AttachmentSize,
            ReplyToId = request.ReplyToId,
            DirectoryId = directoryId,
            OrganizationId = directory.OrganizationId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        return await GetMessageDtoAsync(message.Id);
    }

    public async Task<ChatMessageDto?> EditMessageAsync(Guid messageId, Guid userId, EditMessageRequestDto request)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.UserId == userId);

        if (message == null || message.IsDeleted)
        {
            return null;
        }

        message.Content = request.Content;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetMessageDtoAsync(message.Id);
    }

    public async Task<bool> DeleteMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.UserId == userId);

        if (message == null)
        {
            return false;
        }

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ChatHistoryResponseDto> GetChatHistoryAsync(Guid directoryId, ChatHistoryRequestDto request)
    {
        var query = _context.ChatMessages
            .Where(m => m.DirectoryId == directoryId && !m.IsDeleted)
            .Include(m => m.User)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        if (request.BeforeMessageId.HasValue)
        {
            var beforeMessage = await _context.ChatMessages.FindAsync(request.BeforeMessageId.Value);
            if (beforeMessage != null)
            {
                query = query.Where(m => m.CreatedAt < beforeMessage.CreatedAt);
            }
        }

        var messages = await query
            .Take(request.Limit + 1)
            .ToListAsync();

        var hasMore = messages.Count > request.Limit;
        if (hasMore)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        // Reverse to get chronological order
        messages.Reverse();

        return new ChatHistoryResponseDto
        {
            Messages = messages.Select(MapToChatMessageDto).ToList(),
            HasMore = hasMore,
            OldestMessageId = messages.FirstOrDefault()?.Id
        };
    }

    public async Task<List<ChatMessageDto>> GetPinnedMessagesAsync(Guid directoryId)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.DirectoryId == directoryId && m.IsPinned && !m.IsDeleted)
            .Include(m => m.User)
            .OrderByDescending(m => m.PinnedAt)
            .ToListAsync();

        return messages.Select(MapToChatMessageDto).ToList();
    }

    public async Task<bool> PinMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null || message.IsDeleted)
        {
            return false;
        }

        message.IsPinned = true;
        message.PinnedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnpinMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null)
        {
            return false;
        }

        message.IsPinned = false;
        message.PinnedAt = null;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<OnlineUserDto>> GetOnlineUsersAsync(Guid directoryId)
    {
        // Get users who have sent messages in this directory recently (within last 15 minutes)
        var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);

        var userIds = await _context.ChatMessages
            .Where(m => m.DirectoryId == directoryId && m.CreatedAt >= fifteenMinutesAgo)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new OnlineUserDto
            {
                Id = u.Id,
                Name = u.DisplayName ?? u.Username,
                AvatarUrl = u.AvatarUrl,
                Status = u.Status.ToString()
            })
            .ToListAsync();

        return users;
    }

    private async Task<ChatMessageDto> GetMessageDtoAsync(Guid messageId)
    {
        var message = await _context.ChatMessages
            .Include(m => m.User)
            .Include(m => m.ReplyTo)
            .FirstAsync(m => m.Id == messageId);

        return MapToChatMessageDto(message);
    }

    private ChatMessageDto MapToChatMessageDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            Type = message.Type.ToString(),
            Content = message.Content,
            AttachmentUrl = message.AttachmentUrl,
            AttachmentName = message.AttachmentName,
            AttachmentSize = message.AttachmentSize,
            ReplyToId = message.ReplyToId,
            ReplyToContent = message.ReplyTo?.Content,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt,
            IsPinned = message.IsPinned,
            UserId = message.UserId,
            UserName = message.User?.DisplayName ?? message.User?.Username ?? "Unknown",
            UserAvatarUrl = message.User?.AvatarUrl,
            CreatedAt = message.CreatedAt
        };
    }
}
