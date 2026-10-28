using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

/// <summary>
/// TeraTransfer-style temporary file transfer.
/// Allows quick sharing with or without authentication.
/// Auto-expires after configurable period.
/// </summary>
public class Transfer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique short token for the transfer URL (/t/{token}).</summary>
    [Required]
    [MaxLength(16)]
    public string Token { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>Display name for the transfer bundle.</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>Null if created by anonymous user.</summary>
    public Guid? CreatedByUserId { get; set; }

    public long TotalSizeBytes { get; set; }

    public int FileCount { get; set; }

    public int DownloadCount { get; set; } = 0;

    public int? MaxDownloads { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Default: 7 days from creation.</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<TransferFile> Files { get; set; } = new List<TransferFile>();
}

/// <summary>Individual file within a transfer bundle.</summary>
public class TransferFile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TransferId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MimeType { get; set; }

    public long SizeInBytes { get; set; }

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    // Navigation
    [ForeignKey("TransferId")]
    public virtual Transfer Transfer { get; set; } = null!;
}
