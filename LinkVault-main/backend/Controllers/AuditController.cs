using LinkVaultApi.Dtos;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogListResponseDto>> GetAuditLogs([FromQuery] AuditLogFilterRequestDto filter)
    {
        var orgId = GetCurrentOrganizationId();
        var logs = await _auditService.GetAuditLogsAsync(orgId, filter);
        return Ok(logs);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AuditSummaryDto>> GetAuditSummary()
    {
        var orgId = GetCurrentOrganizationId();
        var summary = await _auditService.GetAuditSummaryAsync(orgId);
        return Ok(summary);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<AuditLogDto>>> GetRecentActivity([FromQuery] int limit = 20)
    {
        var orgId = GetCurrentOrganizationId();
        var activity = await _auditService.GetRecentActivityAsync(orgId, limit);
        return Ok(activity);
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
}
