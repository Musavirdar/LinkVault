using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum RoleType
{
    Admin,
    Manager,
    Member,
    Viewer
}

public class Role
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public RoleType Type { get; set; } = RoleType.Member;
    
    // RBAC Permissions
    public bool CanUpload { get; set; } = true;
    
    public bool CanUpdate { get; set; } = true;
    
    public bool CanDelete { get; set; } = false;
    
    public bool CanExport { get; set; } = true;
    
    public bool CanShare { get; set; } = true;
    
    public bool CanManageUsers { get; set; } = false;
    
    public bool CanManageRoles { get; set; } = false;
    
    public bool CanViewAuditLogs { get; set; } = false;
    
    public bool CanManageSettings { get; set; } = false;
    
    // DLP Rules
    public bool RequireExportApproval { get; set; } = false;
    
    public int? MaxFileSizeMb { get; set; }
    
    public string? AllowedFileTypes { get; set; }
    
    // Foreign keys
    [Required]
    public Guid OrganizationId { get; set; }
    
    // Navigation properties
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    public bool IsSystemRole { get; set; } = false;
}
