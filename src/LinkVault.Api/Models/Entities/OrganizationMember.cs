using LinkVault.Api.Models.Enums;

namespace LinkVault.Api.Models.Entities;

public class OrganizationMember
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}
