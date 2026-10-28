using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

/// <summary>Dropbox-style file version history. Each overwrite saves the old version.</summary>
public class FileVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid ResourceId { get; set; }
    
    public int VersionNumber { get; set; } = 1;
    
    /// <summary>Storage path to this specific version.</summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;
    
    public long SizeInBytes { get; set; }
    
    [MaxLength(64)]
    public string? ChecksumSHA256 { get; set; }
    
    [MaxLength(500)]
    public string? ChangeNote { get; set; }
    
    [Required]
    public Guid CreatedById { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    [ForeignKey("ResourceId")]
    public virtual Resource Resource { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public virtual User CreatedBy { get; set; } = null!;
}
