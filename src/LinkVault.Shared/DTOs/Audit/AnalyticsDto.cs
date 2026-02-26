namespace LinkVault.Shared.DTOs.Audit;

public record ResourceAnalyticsDto(
    Guid ResourceId,
    string ResourceType,
    string ResourceName,
    int TotalViews,
    int TotalDownloads,
    int TotalClicks,
    int TotalShares
);

public record IndividualAnalyticsDto(
    int TotalLinkClicks,
    int TotalFileViews,
    int TotalFileDownloads,
    IEnumerable<LinkAnalyticsDto> TopLinks,
    IEnumerable<FileAnalyticsDto> TopFiles
);

public record LinkAnalyticsDto(
    Guid LinkId,
    string Title,
    string Url,
    int ClickCount
);

public record FileAnalyticsDto(
    Guid FileId,
    string FileName,
    int ViewCount,
    int DownloadCount
);

public record CorporateAnalyticsDto(
    int TotalUsers,
    int ActiveUsersToday,
    int TotalFiles,
    int TotalDirectories,
    long TotalStorageUsed,
    IEnumerable<UserActivityDto> MostActiveUsers,
    IEnumerable<AuditLogDto> RecentActivity,
    Dictionary<string, int> ActivityByDay,
    Dictionary<string, int> ActivityByAction
);

public record UserActivityDto(
    Guid UserId,
    string Email,
    string FullName,
    int ActionCount,
    DateTime LastActiveAt
);
