using LinkVault.Api.Extensions;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Individual user's own audit logs (simplified — own actions only).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await _auditService.GetUserLogsAsync(User.GetUserId(), page, pageSize);
        return Ok(logs);
    }

    /// <summary>
    /// Organization-wide audit trail — Admin only.
    /// </summary>
    [HttpGet("organization/{orgId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrganizationLogs(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _auditService.GetOrganizationLogsAsync(orgId, page, pageSize);
        return Ok(logs);
    }
}
