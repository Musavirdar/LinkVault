namespace LinkVault.Api.Models.Entities;

public class LinkDirectory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsPublic { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public User Owner { get; set; } = null!;
    public LinkDirectory? Parent { get; set; }
    public ICollection<LinkDirectory> SubDirectories { get; set; } = new List<LinkDirectory>();
    public ICollection<Link> Links { get; set; } = new List<Link>();
}
