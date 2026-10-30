using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LinkVaultApi.Models;

namespace LinkVaultApi.Agent.Models;

public enum SafeShareStatus
{
    Clean = 0,
    Warning = 1,
    Blocked = 2
}

public class SafeShareScan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public Guid ScannedByUserId { get; set; }

    [Required]
    public SafeShareStatus Status { get; set; } = SafeShareStatus.Clean;

    [Required]
    public string DetectedThreats { get; set; } = "[]";

    [Required]
    [Range(0, 100)]
    public int RiskScore { get; set; } = 0;

    [MaxLength(2000)]
    public string? ModelVerification { get; set; }

    [Required]
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ResourceId")]
    public virtual Resource Resource { get; set; } = null!;

    [ForeignKey("ScannedByUserId")]
    public virtual User ScannedByUser { get; set; } = null!;
}
