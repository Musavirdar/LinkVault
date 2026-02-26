namespace LinkVault.Api.Models.Entities;

public class Link
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid UserId { get; set; }
    public Guid? DirectoryId { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastClickedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    public User User { get; set; } = null!;
    public LinkDirectory? LinkDirectory { get; set; }
    public ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
    public ICollection<LinkClick> Clicks { get; set; } = new List<LinkClick>();
}
