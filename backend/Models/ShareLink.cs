using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

/// <summary>
/// Granular share link with Dropbox-grade controls:
/// password protection, expiry, download limits, viewer tracking.
/// </summary>
public class ShareLink
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Unique URL token for the share link (e.g., /s/abc123).</summary>
    [Required]
    [MaxLength(64)]
    public string Token { get; set; } = Guid.NewGuid().ToString("N")[..12];
    
    [Required]
    public Guid ResourceId { get; set; }
    
    /// <summary>BCrypt-hashed password. Null = no password.</summary>
    [MaxLength(200)]
    public string? PasswordHash { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>Maximum allowed downloads. Null = unlimited.</summary>
    public int? MaxDownloads { get; set; }
    
    public int CurrentDownloads { get; set; } = 0;
    
    /// <summary>Can the viewer see a preview inline, or must they download?</summary>
    public bool AllowPreview { get; set; } = true;
    
    /// <summary>Require email before allowing download.</summary>
    public bool RequireEmail { get; set; } = false;
    
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid CreatedById { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation
    [ForeignKey("ResourceId")]
    public virtual Resource Resource { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public virtual User CreatedBy { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
}
