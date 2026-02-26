using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;

    public OrganizationService(ApplicationDbContext context, IAuditService audit, IEmailService email)
    {
        _context = context;
        _audit = audit;
        _email = email;
    }

    public async Task<OrganizationDto> CreateAsync(Guid adminUserId, CreateOrganizationRequest request)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Domain = request.Domain,
            DataExportPolicy = DataExportPolicy.Restricted,
            CreatedAt = DateTime.UtcNow
        };

        // Assign creator as Admin
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.IsSystemRole)
            ?? throw new NotFoundException("System Admin role not found");

        var user = await _context.Users.FindAsync(adminUserId)
            ?? throw new NotFoundException("User not found");

        user.OrganizationId = org.Id;

        _context.Organizations.Add(org);
        _context.UserRoles.Add(new UserRole
        {
            UserId = adminUserId,
            RoleId = adminRole.Id,
            OrganizationId = org.Id,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, AuditAction.Create, "Organization", org.Id, org.Name, organizationId: org.Id);

        return MapToDto(org, 1);
    }

    public async Task<OrganizationDto> GetByIdAsync(Guid userId, Guid orgId)
    {
        var org = await _context.Organizations
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == orgId)
            ?? throw new NotFoundException("Organization not found");

        if (org.Members.All(m => m.Id != userId))
            throw new UnauthorizedException("Access denied");

        return MapToDto(org, org.Members.Count);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid adminUserId, Guid orgId, UpdateOrganizationRequest request)
    {
        await EnsureAdminAsync(adminUserId, orgId);

        var org = await _context.Organizations.FindAsync(orgId)
            ?? throw new NotFoundException("Organization not found");

        if (request.Name != null) org.Name = request.Name;
        if (request.Domain != null) org.Domain = request.Domain;
        if (request.DataExportPolicy.HasValue) org.DataExportPolicy = request.DataExportPolicy.Value;
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, AuditAction.Update, "Organization", org.Id, org.Name, organizationId: orgId);

        return MapToDto(org, 0);
    }

    public async Task InviteMemberAsync(Guid adminUserId, Guid orgId, InviteMemberRequest request)
    {
        await EnsureAdminAsync(adminUserId, orgId);

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser?.OrganizationId == orgId)
            throw new ConflictException("User is already a member");

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Token = Guid.NewGuid().ToString("N"),
            OrganizationId = orgId,
            InvitedById = adminUserId,
            RoleId = request.RoleId,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, AuditAction.InviteUser, "Invitation", invitation.Id, request.Email, organizationId: orgId);

        var admin = await _context.Users.FindAsync(adminUserId);
        var org = await _context.Organizations.FindAsync(orgId);
        await _email.SendInvitationAsync(
            request.Email,
            org?.Name ?? "LinkVault Organization",
            admin?.Username ?? "Admin",
            invitation.Token);
    }

    public async Task AcceptInvitationAsync(string token, AcceptInvitationRequest request)
    {
        var invitation = await _context.Invitations
            .Include(i => i.Organization)
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Token == token && i.Status == InvitationStatus.Pending)
            ?? throw new NotFoundException("Invitation not found or already used");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invitation has expired");

        // Create the new user account with 2FA already set up
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = invitation.Email,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            AccountType = AccountType.Corporate,
            OrganizationId = invitation.OrganizationId,
            Is2FASetupComplete = true, // Required for corporate
            TwoFactorEnabled = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = invitation.RoleId,
            OrganizationId = invitation.OrganizationId,
            AssignedAt = DateTime.UtcNow
        });

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid adminUserId, Guid orgId, Guid memberId)
    {
        await EnsureAdminAsync(adminUserId, orgId);

        var member = await _context.Users.FindAsync(memberId)
            ?? throw new NotFoundException("Member not found");

        if (member.OrganizationId != orgId)
            throw new NotFoundException("Member is not in this organization");

        member.OrganizationId = null;

        var userRoles = _context.UserRoles.Where(ur => ur.UserId == memberId && ur.OrganizationId == orgId);
        _context.UserRoles.RemoveRange(userRoles);

        await _context.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, AuditAction.RemoveUser, "User", memberId, member.Username, organizationId: orgId);
    }

    public async Task<IEnumerable<MemberDto>> GetMembersAsync(Guid adminUserId, Guid orgId)
    {
        await EnsureAdminAsync(adminUserId, orgId);

        var members = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.OrganizationId == orgId)
            .ToListAsync();

        return members.Select(m => new MemberDto
        {
            Id = m.Id,
            Username = m.Username,
            Email = m.Email,
            FirstName = m.FirstName,
            LastName = m.LastName,
            AvatarUrl = m.AvatarUrl,
            Roles = m.UserRoles.Where(ur => ur.OrganizationId == orgId)
                               .Select(ur => ur.Role?.Name ?? "").ToList(),
            JoinedAt = m.CreatedAt
        });
    }

    private async Task EnsureAdminAsync(Guid userId, Guid orgId)
    {
        var isAdmin = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.OrganizationId == orgId
                         && ur.Role != null && ur.Role.Name == "Admin");

        if (!isAdmin)
            throw new UnauthorizedException("Admin privileges required");
    }

    private static OrganizationDto MapToDto(Organization org, int memberCount) => new()
    {
        Id = org.Id,
        Name = org.Name,
        Domain = org.Domain,
        DataExportPolicy = org.DataExportPolicy,
        MemberCount = memberCount,
        CreatedAt = org.CreatedAt
    };
}
