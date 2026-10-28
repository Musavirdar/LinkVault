using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

/// <summary>
/// Short-lived, single-use ticket enabling authenticated browser file downloads.
/// </summary>
public class DownloadTicket
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(64)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ResourceId")]
    public virtual Resource Resource { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
