using System.Security.Claims;
using LinkVaultApi.Agent.Models;
using LinkVaultApi.Agent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVaultApi.Agent.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IVaultAgentService _agentService;
    private readonly ISafeShareGuard _safeShareGuard;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IVaultAgentService agentService,
        ISafeShareGuard safeShareGuard,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _safeShareGuard = safeShareGuard;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpPost("plan")]
    public async Task<ActionResult<AgentPlanResponse>> CreatePlan([FromBody] CreatePlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserGoal))
            return BadRequest(new { error = "User goal cannot be empty." });

        try
        {
            var plan = await _agentService.GeneratePlanAsync(request, User);
            return Ok(plan);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent plan");
            return StatusCode(500, new { error = "An error occurred while generating the agent plan." });
        }
    }

    [HttpGet("plan/{planId}")]
    public async Task<ActionResult<AgentPlanResponse>> GetPlan(Guid planId)
    {
        var plan = await _agentService.GetPlanAsync(planId, User);
        if (plan == null) return NotFound(new { error = $"Plan {planId} not found." });
        return Ok(plan);
    }

    [HttpPost("plan/{planId}/approve")]
    public async Task<ActionResult<ApprovePlanResponse>> ApprovePlan(Guid planId)
    {
        try
        {
            var res = await _agentService.ApprovePlanAsync(planId, User);
            return Ok(res);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("plan/{planId}/reject")]
    public async Task<ActionResult<RejectPlanResponse>> RejectPlan(Guid planId)
    {
        try
        {
            var res = await _agentService.RejectPlanAsync(planId, User);
            return Ok(res);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting plan {PlanId}", planId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("safe-share/scan")]
    public async Task<ActionResult<SafeShareScanResponse>> ScanResource([FromBody] SafeShareScanRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _safeShareGuard.ScanResourceAsync(request.ResourceId, userId);
        return Ok(result);
    }

    [HttpPost("transcribe")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TranscribeResponse>> Transcribe(IFormFile? audio)
    {
        if (audio == null || audio.Length == 0)
        {
            return BadRequest(new { error = "No audio stream received." });
        }

        using var stream = audio.OpenReadStream();
        var result = await _agentService.TranscribeAudioAsync(stream, audio.ContentType);
        return Ok(result);
    }
}
