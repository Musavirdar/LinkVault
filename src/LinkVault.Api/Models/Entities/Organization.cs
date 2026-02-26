namespace LinkVault.Api.Models.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public DataExportPolicy DataExportPolicy { get; set; } = DataExportPolicy.Restricted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<UserDirectory> Directories { get; set; } = new List<UserDirectory>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ExportRequest> ExportRequests { get; set; } = new List<ExportRequest>();
    public Subscription? Subscription { get; set; }
}

public enum DataExportPolicy
{
    Open = 0,          // Employees can download freely
    AdminApproval = 1, // Employees must request, Admin approves
    Restricted = 2     // No export allowed
}
