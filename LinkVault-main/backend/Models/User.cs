using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum UserStatus
{
    Online,
    Away,
    Offline,
    DoNotDisturb
}

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? DisplayName { get; set; }
    
    [MaxLength(500)]
    public string? Bio { get; set; }
    
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
    
    // Public profile (Linktree-style)
    /// <summary>Custom URL slug for public profile (e.g., /u/my-custom-slug). Defaults to username.</summary>
    [MaxLength(100)]
    public string? PublicPageSlug { get; set; }
    
    /// <summary>
    /// JSON object storing social media links.
    /// Example: {"twitter":"@user","instagram":"user","github":"user","youtube":"channel","tiktok":"@user","linkedin":"user"}
    /// </summary>
    [MaxLength(5000)]
    public string? SocialLinks { get; set; }
    
    // Authentication
    public string? PasswordHash { get; set; }
    
    public string? PasswordSalt { get; set; }
    
    // SSO
    public string? GoogleId { get; set; }
    
    public string? GitHubId { get; set; }
    
    public string? LinkedInId { get; set; }
    
    // 2FA
    public bool IsTwoFactorEnabled { get; set; } = false;
    
    public string? TwoFactorSecret { get; set; }
    
    public string? TwoFactorBackupCodes { get; set; }
    
    // Status
    public UserStatus Status { get; set; } = UserStatus.Offline;
    
    public DateTime? LastActiveAt { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool EmailVerified { get; set; } = false;
    
    // Foreign keys
    [Required]
    public Guid OrganizationId { get; set; }
    
    public Guid? RoleId { get; set; }
    
    // Navigation properties
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
    
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    
    public virtual ICollection<Link> Links { get; set; } = new List<Link>();
    
    public virtual ProfileTheme? ProfileTheme { get; set; }
}
