namespace LinkVault.Shared.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string Action,
    string EntityType,
    Guid EntityId,
    string? EntityName,
    string? Details,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt
);

public record AuditLogSummaryDto(
    int TotalLogs,
    int TodayLogs,
    Dictionary<string, int> ActionBreakdown,
    Dictionary<string, int> EntityTypeBreakdown,
    IEnumerable<AuditLogDto> RecentLogs
);

public record AuditLogQueryDto(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Action = null,
    string? EntityType = null,
    Guid? UserId = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
);

public record PagedResultDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
