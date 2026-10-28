using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Models;

/// <summary>
/// Granular analytics event for link clicks, file views, and share downloads.
/// Used to build Linktree-grade analytics dashboards.
/// </summary>
public class ClickEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Target (one of these will be set)
    public Guid? LinkId { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid? ShareLinkId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Hashed IP for privacy (SHA-256 truncated).</summary>
    [MaxLength(64)]
    public string? IpHash { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(2000)]
    public string? Referrer { get; set; }

    // Geo (derived from IP via lookup)
    [MaxLength(2)]
    public string? CountryCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    // Device info (parsed from UA)
    [MaxLength(20)]
    public string? DeviceType { get; set; } // Mobile, Desktop, Tablet

    [MaxLength(50)]
    public string? Browser { get; set; }

    [MaxLength(50)]
    public string? OS { get; set; }

    // Owner context
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    [ForeignKey("LinkId")]
    public virtual Link? Link { get; set; }

    [ForeignKey("ResourceId")]
    public virtual Resource? Resource { get; set; }

    [ForeignKey("ShareLinkId")]
    public virtual ShareLink? ShareLink { get; set; }
}
