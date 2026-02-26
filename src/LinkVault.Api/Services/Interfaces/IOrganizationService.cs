using LinkVault.Api.Models.Entities;

namespace LinkVault.Api.Services.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationDto> CreateAsync(Guid adminUserId, CreateOrganizationRequest request);
    Task<OrganizationDto> GetByIdAsync(Guid userId, Guid orgId);
    Task<OrganizationDto> UpdateAsync(Guid adminUserId, Guid orgId, UpdateOrganizationRequest request);
    Task InviteMemberAsync(Guid adminUserId, Guid orgId, InviteMemberRequest request);
    Task AcceptInvitationAsync(string token, AcceptInvitationRequest request);
    Task RemoveMemberAsync(Guid adminUserId, Guid orgId, Guid memberId);
    Task<IEnumerable<MemberDto>> GetMembersAsync(Guid adminUserId, Guid orgId);
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public DataExportPolicy DataExportPolicy { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
}

public class UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public string? Domain { get; set; }
    public DataExportPolicy? DataExportPolicy { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
}

public class AcceptInvitationRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string TwoFactorCode { get; set; } = string.Empty;  // Required — 2FA mandatory
}

public class MemberDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime JoinedAt { get; set; }
}
