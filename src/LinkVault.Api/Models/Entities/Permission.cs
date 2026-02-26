namespace LinkVault.Api.Models.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;  // e.g. "Files", "Links", "Users"
    public string Action { get; set; } = string.Empty;    // e.g. "Read", "Write", "Delete", "Export"

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
