using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum MessageType
{
    Text,
    Image,
    File,
    System
}

public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public MessageType Type { get; set; } = MessageType.Text;
    
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    // For file attachments
    [MaxLength(500)]
    public string? AttachmentUrl { get; set; }
    
    [MaxLength(200)]
    public string? AttachmentName { get; set; }
    
    public long? AttachmentSize { get; set; }
    
    // Threading
    public Guid? ReplyToId { get; set; }
    
    // Reactions (stored as JSON)
    public string? Reactions { get; set; }
    
    // Edit tracking
    public bool IsEdited { get; set; } = false;
    
    public DateTime? EditedAt { get; set; }
    
    // Pinning
    public bool IsPinned { get; set; } = false;
    
    public DateTime? PinnedAt { get; set; }
    
    // Foreign keys
    [Required]
    public Guid DirectoryId { get; set; }
    
    [Required]
    public Guid OrganizationId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    // Navigation properties
    [ForeignKey("DirectoryId")]
    public virtual Directory Directory { get; set; } = null!;
    
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("ReplyToId")]
    public virtual ChatMessage? ReplyTo { get; set; }
    
    public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
}
