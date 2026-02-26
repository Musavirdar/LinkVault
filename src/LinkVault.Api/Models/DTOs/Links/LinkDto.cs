namespace LinkVault.Api.Models.DTOs.Links;

public class LinkDto
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string ShortUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? DirectoryId { get; set; }
    public string? DirectoryName { get; set; }
    public int ClickCount { get; set; }
    public DateTime? LastClickedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}
