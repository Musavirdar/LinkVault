namespace LinkVault.Api.Models.Entities;

public class FileItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? StorageProvider { get; set; }  // "S3", "Azure", "Local"
    public int ViewCount { get; set; } = 0;
    public int DownloadCount { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public Guid DirectoryId { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public UserDirectory Directory { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
    public ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
    public ICollection<ShareLink> ShareLinks { get; set; } = new List<ShareLink>();
    public ICollection<ExportRequest> ExportRequests { get; set; } = new List<ExportRequest>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class FileVersion
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ChangeNote { get; set; }

    public Guid FileItemId { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FileItem FileItem { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
