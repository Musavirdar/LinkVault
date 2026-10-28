using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageRoles")]
public class RolesController : ControllerBase
{
    private readonly LinkVaultDbContext _context;
    private readonly ILogger<RolesController> _logger;

    public RolesController(LinkVaultDbContext context, ILogger<RolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        var orgId = GetCurrentOrganizationId();
        
        var roles = await _context.Roles
            .Where(r => r.OrganizationId == orgId && r.IsActive)
            .Include(r => r.Users)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Type = r.Type.ToString(),
                CanUpload = r.CanUpload,
                CanUpdate = r.CanUpdate,
                CanDelete = r.CanDelete,
                CanExport = r.CanExport,
                CanShare = r.CanShare,
                CanManageUsers = r.CanManageUsers,
                CanManageRoles = r.CanManageRoles,
                CanViewAuditLogs = r.CanViewAuditLogs,
                CanManageSettings = r.CanManageSettings,
                RequireExportApproval = r.RequireExportApproval,
                MaxFileSizeMb = r.MaxFileSizeMb,
                AllowedFileTypes = r.AllowedFileTypes,
                UserCount = r.Users.Count,
                CreatedAt = r.CreatedAt,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetRole(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        
        var role = await _context.Roles
            .Include(r => r.Users)
            .Where(r => r.Id == id && r.OrganizationId == orgId && r.IsActive)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Type = r.Type.ToString(),
                CanUpload = r.CanUpload,
                CanUpdate = r.CanUpdate,
                CanDelete = r.CanDelete,
                CanExport = r.CanExport,
                CanShare = r.CanShare,
                CanManageUsers = r.CanManageUsers,
                CanManageRoles = r.CanManageRoles,
                CanViewAuditLogs = r.CanViewAuditLogs,
                CanManageSettings = r.CanManageSettings,
                RequireExportApproval = r.RequireExportApproval,
                MaxFileSizeMb = r.MaxFileSizeMb,
                AllowedFileTypes = r.AllowedFileTypes,
                UserCount = r.Users.Count,
                CreatedAt = r.CreatedAt,
                IsSystemRole = r.IsSystemRole
            })
            .FirstOrDefaultAsync();

        if (role == null)
        {
            return NotFound();
        }

        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleRequestDto request)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            CanUpload = request.CanUpload,
            CanUpdate = request.CanUpdate,
            CanDelete = request.CanDelete,
            CanExport = request.CanExport,
            CanShare = request.CanShare,
            CanManageUsers = request.CanManageUsers,
            CanManageRoles = request.CanManageRoles,
            CanViewAuditLogs = request.CanViewAuditLogs,
            CanManageSettings = request.CanManageSettings,
            RequireExportApproval = request.RequireExportApproval,
            MaxFileSizeMb = request.MaxFileSizeMb,
            AllowedFileTypes = request.AllowedFileTypes,
            OrganizationId = orgId
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created role {RoleId}", userId, role.Id);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, MapToRoleDto(role));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, UpdateRoleRequestDto request)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);

        if (role == null)
        {
            return NotFound();
        }

        if (role.IsSystemRole)
        {
            return BadRequest(new { error = "Cannot modify system roles" });
        }

        if (request.Name != null)
            role.Name = request.Name;
        if (request.Description != null)
            role.Description = request.Description;
        if (request.CanUpload.HasValue)
            role.CanUpload = request.CanUpload.Value;
        if (request.CanUpdate.HasValue)
            role.CanUpdate = request.CanUpdate.Value;
        if (request.CanDelete.HasValue)
            role.CanDelete = request.CanDelete.Value;
        if (request.CanExport.HasValue)
            role.CanExport = request.CanExport.Value;
        if (request.CanShare.HasValue)
            role.CanShare = request.CanShare.Value;
        if (request.CanManageUsers.HasValue)
            role.CanManageUsers = request.CanManageUsers.Value;
        if (request.CanManageRoles.HasValue)
            role.CanManageRoles = request.CanManageRoles.Value;
        if (request.CanViewAuditLogs.HasValue)
            role.CanViewAuditLogs = request.CanViewAuditLogs.Value;
        if (request.CanManageSettings.HasValue)
            role.CanManageSettings = request.CanManageSettings.Value;
        if (request.RequireExportApproval.HasValue)
            role.RequireExportApproval = request.RequireExportApproval.Value;
        if (request.MaxFileSizeMb.HasValue)
            role.MaxFileSizeMb = request.MaxFileSizeMb.Value;
        if (request.AllowedFileTypes != null)
            role.AllowedFileTypes = request.AllowedFileTypes;

        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated role {RoleId}", userId, role.Id);

        return Ok(MapToRoleDto(role));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();

        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);

        if (role == null)
        {
            return NotFound();
        }

        if (role.IsSystemRole)
        {
            return BadRequest(new { error = "Cannot delete system roles" });
        }

        if (role.Users.Any())
        {
            return BadRequest(new { error = "Cannot delete role with assigned users" });
        }

        role.IsActive = false;
        role.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted role {RoleId}", userId, role.Id);

        return NoContent();
    }

    [HttpPost("assign")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult> AssignRole(AssignRoleRequestDto request)
    {
        var orgId = GetCurrentOrganizationId();
        var userId = GetCurrentUserId();

        var org = await _context.Organizations
            .Include(o => o.Users)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(o => o.Id == orgId);

        if (org == null)
            return NotFound(new { error = "Organization not found" });

        // ─── Individual mode block: no team members allowed ────────────────────
        if (org.AccountType == AccountType.Individual)
            return BadRequest(new { error = "Individual accounts cannot add team members." });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == orgId);

        if (user == null)
            return NotFound(new { error = "User not found" });

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.OrganizationId == orgId);

        if (role == null)
            return NotFound(new { error = "Role not found" });

        // ─── Admin cap enforcement ─────────────────────────────────────────────
        if (role.Type == RoleType.Admin)
        {
            var currentAdminCount = org.Users
                .Count(u => u.Role?.Type == RoleType.Admin && u.Id != user.Id);

            if (currentAdminCount >= org.MaxAdmins)
            {
                return BadRequest(new
                {
                    error = $"Admin limit reached ({org.MaxAdmins} admins allowed on your plan). Upgrade your subscription to add more admins."
                });
            }
        }

        user.RoleId = role.Id;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} assigned role {RoleId} to user {TargetUserId}", 
            userId, role.Id, user.Id);

        return Ok(new { message = "Role assigned successfully" });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }

    private Guid GetCurrentOrganizationId()
    {
        var orgIdClaim = User.FindFirst("orgId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var orgId))
        {
            throw new UnauthorizedAccessException("Invalid organization ID");
        }
        return orgId;
    }

    private RoleDto MapToRoleDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Type = role.Type.ToString(),
            CanUpload = role.CanUpload,
            CanUpdate = role.CanUpdate,
            CanDelete = role.CanDelete,
            CanExport = role.CanExport,
            CanShare = role.CanShare,
            CanManageUsers = role.CanManageUsers,
            CanManageRoles = role.CanManageRoles,
            CanViewAuditLogs = role.CanViewAuditLogs,
            CanManageSettings = role.CanManageSettings,
            RequireExportApproval = role.RequireExportApproval,
            MaxFileSizeMb = role.MaxFileSizeMb,
            AllowedFileTypes = role.AllowedFileTypes,
            UserCount = role.Users?.Count ?? 0,
            CreatedAt = role.CreatedAt,
            IsSystemRole = role.IsSystemRole
        };
    }
}
