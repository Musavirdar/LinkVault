namespace LinkVault.Api.Models.Entities;

public class Invitation
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid InvitedById { get; set; }
    public Guid RoleId { get; set; }  // Which role to assign on acceptance
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public User InvitedBy { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}
