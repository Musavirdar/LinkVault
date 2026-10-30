using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum AuditActionType
{
    Created,
    Updated,
    Deleted,
    Viewed,
    Clicked,
    Opened,
    Changed,
    Shared,
    Exported,
    Downloaded,
    Uploaded,
    Login,
    Logout,
    PasswordChanged,
    TwoFactorEnabled,
    TwoFactorDisabled,
    RoleChanged,
    SettingsChanged
}

public enum ResourceEntityType
{
    User,
    Organization,
    Directory,
    Resource,
    Link,
    Role,
    Settings,
    System
}

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public AuditActionType ActionType { get; set; }
    
    [Required]
    public ResourceEntityType EntityType { get; set; }
    
    public Guid? EntityId { get; set; }
    
    [MaxLength(100)]
    public string? EntityName { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    // Old and new values for changes
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    // IP and user agent for tracking
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    // Foreign keys
    [Required]
    public Guid OrganizationId { get; set; }
    
    public Guid? UserId { get; set; }
    
    public Guid? ResourceId { get; set; }
    
    // Navigation properties
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
    
    [ForeignKey("ResourceId")]
    public virtual Resource? Resource { get; set; }
    
    // Agent execution provenance
    public bool IsViaAgent { get; set; } = false;
    
    public Guid? AgentPlanId { get; set; }
    
    [MaxLength(100)]
    public string? AgentToolName { get; set; }
    
    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
