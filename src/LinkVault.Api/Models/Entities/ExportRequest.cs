namespace LinkVault.Api.Models.Entities;

/// <summary>
/// DLP export request — employees request to download/export a file,
/// admins approve or deny based on the org's DataExportPolicy.
/// </summary>
public class ExportRequest
{
    public Guid Id { get; set; }
    public Guid FileItemId { get; set; }
    public Guid RequestedById { get; set; }
    public string? Reason { get; set; }
    public ExportRequestStatus Status { get; set; } = ExportRequestStatus.Pending;

    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FileItem FileItem { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public User? ReviewedBy { get; set; }
    public Organization Organization { get; set; } = null!;
}

public enum ExportRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2
}
