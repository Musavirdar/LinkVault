using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum ResourceType
{
    File,
    Link,
    Folder
}

public enum FileType
{
    Image,
    Document,
    Video,
    Audio,
    Archive,
    Other
}

public class Resource
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public ResourceType Type { get; set; } = ResourceType.File;
    
    public FileType? FileType { get; set; }
    
    // For files
    [MaxLength(500)]
    public string? StoragePath { get; set; }
    
    [MaxLength(500)]
    public string? MimeType { get; set; }
    
    public long? SizeInBytes { get; set; }
    
    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
    
    // For links
    [MaxLength(2000)]
    public string? Url { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Analytics
    public int ViewCount { get; set; } = 0;
    
    public int ClickCount { get; set; } = 0;
    
    public DateTime? LastAccessedAt { get; set; }
    
    public int DownloadCount { get; set; } = 0;
    
    // Versioning (Dropbox-style)
    public int VersionCount { get; set; } = 1;
    
    // Integrity
    /// <summary>SHA-256 checksum of the file for integrity verification.</summary>
    [MaxLength(64)]
    public string? ChecksumSHA256 { get; set; }
    
    // Settings
    public bool IsPublic { get; set; } = false;
    
    public string? Password { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    // Foreign keys
    private Guid? _directoryId;
    public Guid? DirectoryId
    {
        get => _directoryId;
        set => _directoryId = (value == Guid.Empty) ? null : value;
    }
    
    [Required]
    public Guid OrganizationId { get; set; }
    
    [Required]
    public Guid CreatedById { get; set; }
    
    // Navigation properties
    [ForeignKey("DirectoryId")]
    public virtual Directory? Directory { get; set; }
    
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public virtual User CreatedBy { get; set; } = null!;
    
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
}
