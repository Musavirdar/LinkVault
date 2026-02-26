namespace LinkVault.Api.Models.Entities;

public class UserDirectory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public Guid? ParentDirectoryId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Owner { get; set; } = null!;
    public UserDirectory? ParentDirectory { get; set; }
    public Organization? Organization { get; set; }
    public ICollection<UserDirectory> SubDirectories { get; set; } = new List<UserDirectory>();
    public ICollection<Link> Links { get; set; } = new List<Link>();
}
