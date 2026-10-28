using LinkVaultApi.Dtos;
using LinkVaultApi.Models;

namespace LinkVaultApi.Services;

public interface IAuditService
{
    Task LogAsync(AuditActionType actionType, ResourceEntityType entityType, Guid? entityId, 
        string? entityName, string? description, string? oldValues, string? newValues,
        Guid organizationId, Guid? userId, string? ipAddress, string? userAgent);
    
    Task<AuditLogListResponseDto> GetAuditLogsAsync(Guid organizationId, AuditLogFilterRequestDto filter);
    Task<AuditSummaryDto> GetAuditSummaryAsync(Guid organizationId);
    Task<List<AuditLogDto>> GetRecentActivityAsync(Guid organizationId, int limit = 20);
}
