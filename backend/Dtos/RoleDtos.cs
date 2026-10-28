using System.ComponentModel.DataAnnotations;
using LinkVaultApi.Models;

namespace LinkVaultApi.Dtos;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool CanUpload { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanShare { get; set; }
    public bool CanManageUsers { get; set; }
    public bool CanManageRoles { get; set; }
    public bool CanViewAuditLogs { get; set; }
    public bool CanManageSettings { get; set; }
    public bool RequireExportApproval { get; set; }
    public int? MaxFileSizeMb { get; set; }
    public string? AllowedFileTypes { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSystemRole { get; set; }
}

public class CreateRoleRequestDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public RoleType Type { get; set; } = RoleType.Member;

    public bool CanUpload { get; set; } = true;
    public bool CanUpdate { get; set; } = true;
    public bool CanDelete { get; set; } = false;
    public bool CanExport { get; set; } = true;
    public bool CanShare { get; set; } = true;
    public bool CanManageUsers { get; set; } = false;
    public bool CanManageRoles { get; set; } = false;
    public bool CanViewAuditLogs { get; set; } = false;
    public bool CanManageSettings { get; set; } = false;
    public bool RequireExportApproval { get; set; } = false;
    public int? MaxFileSizeMb { get; set; }
    public string? AllowedFileTypes { get; set; }
}

public class UpdateRoleRequestDto
{
    [MaxLength(50)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool? CanUpload { get; set; }
    public bool? CanUpdate { get; set; }
    public bool? CanDelete { get; set; }
    public bool? CanExport { get; set; }
    public bool? CanShare { get; set; }
    public bool? CanManageUsers { get; set; }
    public bool? CanManageRoles { get; set; }
    public bool? CanViewAuditLogs { get; set; }
    public bool? CanManageSettings { get; set; }
    public bool? RequireExportApproval { get; set; }
    public int? MaxFileSizeMb { get; set; }
    public string? AllowedFileTypes { get; set; }
}

public class AssignRoleRequestDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }
}
