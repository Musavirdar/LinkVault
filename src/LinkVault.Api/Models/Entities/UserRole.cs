namespace LinkVault.Api.Models.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Organization? Organization { get; set; }
}
