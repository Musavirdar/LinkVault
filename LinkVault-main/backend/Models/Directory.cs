using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

public enum ExportRule
{
    Allow,
    RequireApproval,
    Deny
}

public class Directory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? Icon { get; set; }
    
    [MaxLength(7)]
    public string? Color { get; set; }
    
    // Settings
    public bool IsPublic { get; set; } = false;
    
    public ExportRule ExportRule { get; set; } = ExportRule.Allow;
    
    // Hierarchy
    private Guid? _parentDirectoryId;
    public Guid? ParentDirectoryId
    {
        get => _parentDirectoryId;
        set => _parentDirectoryId = (value == Guid.Empty) ? null : value;
    }
    
    // Foreign keys
    [Required]
    public Guid OrganizationId { get; set; }
    
    public Guid? CreatedById { get; set; }
    
    // Navigation properties
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;
    
    [ForeignKey("CreatedById")]
    public virtual User? CreatedBy { get; set; }
    
    [ForeignKey("ParentDirectoryId")]
    public virtual Directory? ParentDirectory { get; set; }
    
    public virtual ICollection<Directory> SubDirectories { get; set; } = new List<Directory>();
    
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
