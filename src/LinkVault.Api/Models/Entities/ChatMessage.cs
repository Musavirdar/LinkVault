namespace LinkVault.Api.Models.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChatType ChatType { get; set; }
    public Guid SenderId { get; set; }
    public string? SenderName { get; set; }

    // Either tied to a directory (project chat) or org (general chat)
    public Guid? DirectoryId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }

    // Navigation
    public User Sender { get; set; } = null!;
    public UserDirectory? Directory { get; set; }
    public Organization? Organization { get; set; }
}

public enum ChatType
{
    DirectoryChat = 0,   // Project-specific chat tied to a directory
    OrganizationChat = 1 // Organization-wide general chat
}
