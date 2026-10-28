using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Services;

public class AuditService : IAuditService
{
    private readonly LinkVaultDbContext _context;

    public AuditService(LinkVaultDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        AuditActionType actionType,
        ResourceEntityType entityType,
        Guid? entityId,
        string? entityName,
        string? description,
        string? oldValues,
        string? newValues,
        Guid organizationId,
        Guid? userId,
        string? ipAddress,
        string? userAgent)
    {
        var auditLog = new AuditLog
        {
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            OrganizationId = organizationId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<AuditLogListResponseDto> GetAuditLogsAsync(Guid organizationId, AuditLogFilterRequestDto filter)
    {
        var query = _context.AuditLogs
            .Where(a => a.OrganizationId == organizationId)
            .AsQueryable();

        // Apply filters
        if (filter.ActionType.HasValue)
            query = query.Where(a => a.ActionType == filter.ActionType.Value);

        if (filter.EntityType.HasValue)
            query = query.Where(a => a.EntityType == filter.EntityType.Value);

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        if (filter.EntityId.HasValue)
            query = query.Where(a => a.EntityId == filter.EntityId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);

        // Order by date descending
        query = query.OrderByDescending(a => a.CreatedAt);

        // Get total count
        var totalCount = await query.CountAsync();

        // Materialise, then map in memory to avoid EF Core instance-method projection error
        var raw = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(a => a.User)
            .ToListAsync();

        var items = raw.Select(MapToAuditLogDto).ToList();

        return new AuditLogListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<AuditSummaryDto> GetAuditSummaryAsync(Guid organizationId)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var todayCount = await _context.AuditLogs
            .CountAsync(a => a.OrganizationId == organizationId && a.CreatedAt >= today);

        var weekCount = await _context.AuditLogs
            .CountAsync(a => a.OrganizationId == organizationId && a.CreatedAt >= weekAgo);

        var monthCount = await _context.AuditLogs
            .CountAsync(a => a.OrganizationId == organizationId && a.CreatedAt >= monthAgo);

        var actionsByType = await _context.AuditLogs
            .Where(a => a.OrganizationId == organizationId && a.CreatedAt >= monthAgo)
            .GroupBy(a => a.ActionType)
            .Select(g => new { Action = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        var actionsByUser = await _context.AuditLogs
            .Where(a => a.OrganizationId == organizationId && a.CreatedAt >= monthAgo && a.UserId != null)
            .Include(a => a.User)
            .GroupBy(a => a.User!.DisplayName ?? a.User.Username)
            .Select(g => new { User = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.User, x => x.Count);

        return new AuditSummaryDto
        {
            TotalActionsToday = todayCount,
            TotalActionsThisWeek = weekCount,
            TotalActionsThisMonth = monthCount,
            ActionsByType = actionsByType,
            ActionsByUser = actionsByUser
        };
    }

    public async Task<List<AuditLogDto>> GetRecentActivityAsync(Guid organizationId, int limit = 20)
    {
        // Materialise first, then map — avoids EF Core instance-method LINQ translation error
        var raw = await _context.AuditLogs
            .Where(a => a.OrganizationId == organizationId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return raw.Select(MapToAuditLogDto).ToList();
    }

    private static AuditLogDto MapToAuditLogDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            ActionType = auditLog.ActionType.ToString(),
            EntityType = auditLog.EntityType.ToString(),
            EntityId = auditLog.EntityId,
            EntityName = auditLog.EntityName,
            Description = auditLog.Description,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            IpAddress = auditLog.IpAddress,
            UserId = auditLog.UserId,
            UserName = auditLog.User?.DisplayName ?? auditLog.User?.Username,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
