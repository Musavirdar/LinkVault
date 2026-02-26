using LinkVault.Api.Models.Entities;

namespace LinkVault.Api.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid userId, AuditAction action, string entityType,
        Guid? entityId = null, string? entityName = null, string? details = null,
        Guid? organizationId = null);

    Task<IEnumerable<AuditLog>> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetOrganizationLogsAsync(Guid organizationId, int page = 1, int pageSize = 50);
}
