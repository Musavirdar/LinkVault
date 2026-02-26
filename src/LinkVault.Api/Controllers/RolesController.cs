using LinkVault.Api.Data;
using LinkVault.Api.Extensions;
using LinkVault.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Controllers;

/// <summary>
/// RBAC role assignment management for admin users.
/// Roles are created per-organization (or system-wide via seed data).
/// </summary>
[ApiController]
[Route("api/organizations/{orgId}/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context) => _context = context;

    /// <summary>List all roles available in this organization (system + org-specific).</summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles(Guid orgId)
    {
        var roles = await _context.Roles
            .Where(r => r.IsSystemRole || r.OrganizationId == orgId)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync();

        return Ok(roles);
    }

    /// <summary>Create a custom role for this organization.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole(Guid orgId, [FromBody] CreateRoleRequest request)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OrganizationId = orgId,
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRoles), new { orgId }, new RoleDto
        {
            Id = role.Id, Name = role.Name, Description = role.Description, IsSystemRole = false
        });
    }

    /// <summary>Assign a role to a member of this organization.</summary>
    [HttpPost("{roleId}/assign/{memberId}")]
    public async Task<IActionResult> AssignRole(Guid orgId, Guid roleId, Guid memberId)
    {
        var member = await _context.Users.FindAsync(memberId);
        if (member == null || member.OrganizationId != orgId)
            return NotFound(new { error = "Member not found in this organization" });

        var role = await _context.Roles.FindAsync(roleId);
        if (role == null || (role.OrganizationId != null && role.OrganizationId != orgId && !role.IsSystemRole))
            return NotFound(new { error = "Role not found" });

        var existing = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == memberId && ur.RoleId == roleId && ur.OrganizationId == orgId);

        if (!existing)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = memberId,
                RoleId = roleId,
                OrganizationId = orgId,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Role assigned" });
    }

    /// <summary>Revoke a role from a member.</summary>
    [HttpDelete("{roleId}/assign/{memberId}")]
    public async Task<IActionResult> RevokeRole(Guid orgId, Guid roleId, Guid memberId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == memberId && ur.RoleId == roleId && ur.OrganizationId == orgId);

        if (userRole == null)
            return NotFound(new { error = "Role assignment not found" });

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>List all role assignments for an org member.</summary>
    [HttpGet("members/{memberId}")]
    public async Task<IActionResult> GetMemberRoles(Guid orgId, Guid memberId)
    {
        var roles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == memberId && ur.OrganizationId == orgId)
            .Select(ur => new RoleDto
            {
                Id = ur.Role!.Id,
                Name = ur.Role.Name,
                Description = ur.Role.Description,
                IsSystemRole = ur.Role.IsSystemRole
            })
            .ToListAsync();

        return Ok(roles);
    }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
