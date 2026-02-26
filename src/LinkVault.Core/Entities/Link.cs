namespace LinkVault.Core.Entities;

public class Link : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int ClickCount { get; set; } = 0;
    
    public Guid? DirectoryId { get; set; }
    public Directory? Directory { get; set; }
    
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
}
