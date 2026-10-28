using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum LinkType
{
    Standard,
    Header,
    SocialIcon,
    Embed,
    Image
}

public class Link
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    [MaxLength(50)]
    public string? Icon { get; set; }
    
    // Link type (Standard, Header, SocialIcon, Embed, Image)
    [Required]
    public LinkType LinkType { get; set; } = LinkType.Standard;
    
    // Embed support (YouTube, Spotify, etc.)
    [MaxLength(5000)]
    public string? EmbedHtml { get; set; }
    
    // Display order
    public int DisplayOrder { get; set; } = 0;
    
    // Analytics
    public int ClickCount { get; set; } = 0;
    
    public DateTime? LastClickedAt { get; set; }
    
    // Settings
    public bool IsActive { get; set; } = true;
    
    public bool IsFeatured { get; set; } = false;
    
    /// <summary>Manual show/hide toggle independent of scheduling.</summary>
    public bool IsVisible { get; set; } = true;
    
    // Scheduling (Linktree-style)
    /// <summary>Link becomes visible at this time. Null = immediately visible.</summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>Link auto-hides after this time. Null = never expires.</summary>
    public DateTime? ExpiresAt { get; set; }
    
    // Code lock (Linktree-style 4-digit PIN)
    /// <summary>If set, visitors must enter this PIN to access the link.</summary>
    [MaxLength(4)]
    public string? CodeLock { get; set; }
    
    // Animation style for entrance on public profile
    [MaxLength(30)]
    public string? AnimationStyle { get; set; }
    
    // Foreign keys
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid OrganizationId { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
