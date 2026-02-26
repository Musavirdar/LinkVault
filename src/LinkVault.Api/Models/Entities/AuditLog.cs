namespace LinkVault.Api.Models.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;  // "File", "Link", "Directory", "User"
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Organization? Organization { get; set; }
}

public enum AuditAction
{
    Create = 0,
    Read = 1,
    Update = 2,
    Delete = 3,
    Login = 4,
    Logout = 5,
    Export = 6,
    Share = 7,
    Download = 8,
    Upload = 9,
    InviteUser = 10,
    RemoveUser = 11,
    ChangePermission = 12,
    ExportRequest = 13,
    ExportApproved = 14,
    ExportDenied = 15
}
