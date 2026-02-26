using LinkVault.Api.Data;
using LinkVault.Api.Extensions;
using LinkVault.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SubscriptionsController(ApplicationDbContext context) => _context = context;

    /// <summary>Get the current user's subscription details.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = User.GetUserId();
        var sub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (sub == null)
            return Ok(new { tier = "IndividualFree", status = "Active", message = "No paid subscription" });

        return Ok(MapToDto(sub));
    }

    /// <summary>Get an organization's subscription (Admin only).</summary>
    [HttpGet("organization/{orgId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrgSubscription(Guid orgId)
    {
        var sub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == orgId);

        if (sub == null)
            return Ok(new { tier = "CorporateBasic", status = "Trial", message = "No paid subscription" });

        return Ok(MapToDto(sub));
    }

    /// <summary>
    /// Upgrade / change subscription tier.
    /// In production: call Stripe here before persisting the tier change.
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeRequest request)
    {
        var userId = User.GetUserId();
        var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);

        if (sub == null)
        {
            sub = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Tier = request.Tier,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                TrialEndsAt = DateTime.UtcNow, // no trial when manually upgrading
                CreatedAt = DateTime.UtcNow
            };
            _context.Subscriptions.Add(sub);
        }
        else
        {
            sub.Tier = request.Tier;
            sub.Status = SubscriptionStatus.Active;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(MapToDto(sub));
    }

    /// <summary>Cancel subscription at end of current period.</summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel()
    {
        var userId = User.GetUserId();
        var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);

        if (sub == null)
            return NotFound(new { error = "No active subscription found" });

        sub.Status = SubscriptionStatus.Cancelled;
        sub.EndDate = DateTime.UtcNow;
        sub.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscription cancelled successfully." });
    }

    private static SubscriptionDto MapToDto(Subscription s) => new()
    {
        Id = s.Id,
        Tier = s.Tier.ToString(),
        Status = s.Status.ToString(),
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        TrialEndsAt = s.TrialEndsAt,
        IsTrialActive = s.Status == SubscriptionStatus.Trial && s.TrialEndsAt > DateTime.UtcNow,
        StripeCustomerId = s.StripeCustomerId
    };
}

public class UpgradeRequest
{
    public SubscriptionTier Tier { get; set; }
}

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime TrialEndsAt { get; set; }
    public bool IsTrialActive { get; set; }
    public string? StripeCustomerId { get; set; }
}
