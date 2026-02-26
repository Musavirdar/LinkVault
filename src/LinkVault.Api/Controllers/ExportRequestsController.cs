using LinkVault.Api.Extensions;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/export-requests")]
[Authorize]
public class ExportRequestsController : ControllerBase
{
    private readonly IExportRequestService _exportService;

    public ExportRequestsController(IExportRequestService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// Employee: request permission to download a file.
    /// Only applicable when org DataExportPolicy is AdminApproval.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExportRequestDto request)
    {
        var result = await _exportService.CreateRequestAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetMyRequests), result);
    }

    /// <summary>Employee: see status of your own requests.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRequests()
    {
        var result = await _exportService.GetMyRequestsAsync(User.GetUserId());
        return Ok(result);
    }

    /// <summary>Admin: see all pending export requests for an organization.</summary>
    [HttpGet("organization/{orgId}/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending(Guid orgId)
    {
        var result = await _exportService.GetPendingForOrgAsync(User.GetUserId(), orgId);
        return Ok(result);
    }

    /// <summary>Admin: approve an export request. Employee receives email notification.</summary>
    [HttpPost("{requestId}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid requestId, [FromBody] ReviewExportRequest body)
    {
        var result = await _exportService.ApproveAsync(User.GetUserId(), requestId, body.Notes);
        return Ok(result);
    }

    /// <summary>Admin: deny an export request. Employee receives email notification.</summary>
    [HttpPost("{requestId}/deny")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deny(Guid requestId, [FromBody] ReviewExportRequest body)
    {
        var result = await _exportService.DenyAsync(User.GetUserId(), requestId, body.Notes ?? "No reason provided");
        return Ok(result);
    }
}

public class ReviewExportRequest
{
    public string? Notes { get; set; }
}
