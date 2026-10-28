using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum AccountType
{
    Individual,
    Corporate
}

public enum SubscriptionTier
{
    Free,
    Pro,
    Enterprise
}

public class Organization
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;
    
    [Required]
    public AccountType AccountType { get; set; } = AccountType.Individual;
    
    [Required]
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Admin limits (enforced per subscription tier)
    /// <summary>
    /// Maximum number of admin-role users allowed. Default 3 (Free/Pro), scalable by subscription.
    /// Free Individual: 1, Free Team: 3, Pro: 5, Enterprise: 10
    /// </summary>
    public int MaxAdmins { get; set; } = 3;

    // Storage quotas (Terabox-style limits per subscription)
    /// <summary>Total bytes currently consumed across all files.</summary>
    public long StorageUsedBytes { get; set; } = 0;
    
    /// <summary>Maximum storage in bytes. Free: 1GB, Pro: 100GB, Enterprise: 1TB.</summary>
    public long StorageLimitBytes { get; set; } = 1L * 1024 * 1024 * 1024; // 1GB default
    
    /// <summary>Maximum single file upload size in bytes. Free: 25MB, Pro: 500MB, Enterprise: 2GB.</summary>
    public long MaxFileSizeBytes { get; set; } = 25L * 1024 * 1024; // 25MB default

    // Corporate settings
    public bool RequireExportApproval { get; set; } = false;
    
    public bool AllowEmployeeUploads { get; set; } = true;
    
    public bool EnableAuditLogging { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    
    public virtual ICollection<Directory> Directories { get; set; } = new List<Directory>();
    
    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
