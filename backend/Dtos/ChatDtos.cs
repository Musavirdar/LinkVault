using System.ComponentModel.DataAnnotations;
using LinkVaultApi.Models;

namespace LinkVaultApi.Dtos;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public long? AttachmentSize { get; set; }
    public Guid? ReplyToId { get; set; }
    public string? ReplyToContent { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsPinned { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SendMessageRequestDto
{
    [Required]
    public MessageType Type { get; set; } = MessageType.Text;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public Guid? ReplyToId { get; set; }

    // For file attachments
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public long? AttachmentSize { get; set; }
}

public class EditMessageRequestDto
{
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}

public class ChatHistoryRequestDto
{
    public Guid? BeforeMessageId { get; set; }
    public int Limit { get; set; } = 50;
}

public class ChatHistoryResponseDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public bool HasMore { get; set; }
    public Guid? OldestMessageId { get; set; }
}

public class TypingIndicatorDto
{
    public Guid DirectoryId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

public class OnlineUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
}
