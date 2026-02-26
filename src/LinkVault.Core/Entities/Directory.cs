namespace LinkVault.Core.Entities;

public class Directory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDirectoryId { get; set; }
    public Directory? ParentDirectory { get; set; }
    
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    // Navigation properties
    public ICollection<Directory> SubDirectories { get; set; } = new List<Directory>();
    public ICollection<FileItem> Files { get; set; } = new List<FileItem>();
    public ICollection<Link> Links { get; set; } = new List<Link>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
