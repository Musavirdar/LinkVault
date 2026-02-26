namespace LinkVault.Api.Models.Entities;

public class ShareLink
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid FileId { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxDownloads { get; set; }
    public int DownloadCount { get; set; } = 0;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FileItem File { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
