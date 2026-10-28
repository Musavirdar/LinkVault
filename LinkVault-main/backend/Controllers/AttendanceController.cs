using LinkVaultApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly LinkVaultDbContext _db;

    public AttendanceController(LinkVaultDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns daily activity counts for the last 365 days, aggregated from AuditLogs.
    /// Shape matches GitHub contribution graph data.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHeatmap()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var since = DateTime.UtcNow.AddDays(-364).Date;

        var daily = await _db.AuditLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= since)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync();

        var result = new List<object>();
        var cursor = since;
        var lookup = daily.ToDictionary(d => d.date, d => d.count);

        while (cursor <= DateTime.UtcNow.Date)
        {
            result.Add(new { date = cursor.ToString("yyyy-MM-dd"), count = lookup.TryGetValue(cursor, out var c) ? c : 0 });
            cursor = cursor.AddDays(1);
        }

        var totalDaysActive = lookup.Count;
        var maxCount = lookup.Values.DefaultIfEmpty(0).Max();
        var totalActions = lookup.Values.Sum();

        var streak = 0;
        var today = DateTime.UtcNow.Date;
        while (lookup.ContainsKey(today) && lookup[today] > 0) { streak++; today = today.AddDays(-1); }

        return Ok(new
        {
            days = result,
            summary = new { totalDaysActive, totalActions, maxSingleDay = maxCount, currentStreak = streak }
        });
    }

    /// <summary>
    /// Returns a quick summary card: streak, total days, and busiest day.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var since = DateTime.UtcNow.AddDays(-364).Date;

        var daily = await _db.AuditLogs
            .Where(a => a.UserId == userId && a.CreatedAt >= since)
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new { date = g.Key, count = g.Count() })
            .ToListAsync();

        var totalDaysActive = daily.Count;
        var totalActions = daily.Sum(d => d.count);
        var maxDay = daily.OrderByDescending(d => d.count).FirstOrDefault();
        var streak = 0;
        var lookup = daily.ToDictionary(d => d.date, d => d.count);
        var today = DateTime.UtcNow.Date;
        while (lookup.ContainsKey(today) && lookup[today] > 0) { streak++; today = today.AddDays(-1); }

        return Ok(new
        {
            totalDaysActive,
            totalActions,
            currentStreak = streak,
            busiestDay = maxDay != null ? new { date = maxDay.date.ToString("yyyy-MM-dd"), count = maxDay.count } : null
        });
    }

    /// <summary>
    /// Returns all active org members with their online status.
    /// A user is considered "online" if their LastActiveAt is within the last 15 minutes.
    /// </summary>
    [HttpGet("online")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnlineMembers()
    {
        var orgIdClaim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(orgIdClaim, out var orgId))
            return Unauthorized();

        var onlineThreshold = DateTime.UtcNow.AddMinutes(-15);

        var members = await _db.Users
            .Where(u => u.OrganizationId == orgId && u.IsActive)
            .Select(u => new
            {
                id = u.Id.ToString(),
                displayName = u.DisplayName ?? u.Username,
                username = u.Username,
                avatarUrl = u.AvatarUrl,
                isOnline = u.LastActiveAt != null && u.LastActiveAt >= onlineThreshold,
                lastSeen = u.LastActiveAt.HasValue ? u.LastActiveAt.Value.ToString("o") : null
            })
            .OrderByDescending(u => u.isOnline)
            .ThenBy(u => u.displayName)
            .ToListAsync();

        return Ok(members);
    }
}
