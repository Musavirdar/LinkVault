using LinkVault.Api.Data;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(Guid userId, AuditAction action, string entityType,
        Guid? entityId = null, string? entityName = null, string? details = null,
        Guid? organizationId = null)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var ua = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
        var user = await _context.Users.FindAsync(userId);

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = user?.Username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Details = details,
            IpAddress = ip,
            UserAgent = ua,
            OrganizationId = organizationId ?? user?.OrganizationId,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        return await _context.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetOrganizationLogsAsync(Guid organizationId, int page = 1, int pageSize = 50)
    {
        return await _context.AuditLogs
            .Where(l => l.OrganizationId == organizationId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
