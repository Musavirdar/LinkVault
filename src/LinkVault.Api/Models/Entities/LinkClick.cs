namespace LinkVault.Api.Models.Entities;

public class LinkClick
{
    public Guid Id { get; set; }
    public Guid LinkId { get; set; }
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    
    public Link Link { get; set; } = null!;
}
