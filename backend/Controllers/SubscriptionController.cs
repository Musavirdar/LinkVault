using LinkVaultApi.Data;
using LinkVaultApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using OrgAccountType = LinkVaultApi.Models.AccountType;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly LinkVaultDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        LinkVaultDbContext context,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? "sk_test_placeholder";
    }

    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        var plans = new[]
        {
            new { Id = "plan_free", Name = "Free", Price = 0.00, Interval = "forever", StripePriceId = (string?)null },
            new { Id = "plan_pro", Name = "Pro", Price = 9.99, Interval = "month", StripePriceId = _configuration["Stripe:ProPriceId"] },
            new { Id = "plan_team", Name = "Team", Price = 29.99, Interval = "month", StripePriceId = _configuration["Stripe:TeamPriceId"] }
        };

        return Ok(plans);
    }

    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var user = await _context.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound("User not found");

        var domain = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?.FirstOrDefault() ?? "http://localhost:5173";

        string priceId = request.PlanId switch
        {
            "plan_pro" => _configuration["Stripe:ProPriceId"] ?? "price_placeholder_pro",
            "plan_team" => _configuration["Stripe:TeamPriceId"] ?? "price_placeholder_team",
            _ => null
        };

        if (priceId == null) return BadRequest("Invalid plan selected for checkout.");

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{domain}/dashboard?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/subscription",
            ClientReferenceId = user.OrganizationId.ToString(), // Link the subscription to the organization
            CustomerEmail = user.Email
        };

        var service = new SessionService();
        try
        {
            Session session = await service.CreateAsync(options);
            return Ok(new { SessionId = session.Id, CheckoutUrl = session.Url });
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe checkout session creation failed");
            return BadRequest(new { Error = e.StripeError.Message });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"] ?? "whsec_placeholder";

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && Guid.TryParse(session.ClientReferenceId, out Guid orgId))
                {
                    var organization = await _context.Organizations.FindAsync(orgId);
                    if (organization != null)
                    {
                        var proPriceId   = _configuration["Stripe:ProPriceId"]  ?? "";
                        var teamPriceId  = _configuration["Stripe:TeamPriceId"] ?? "";

                        // Retrieve line items via the session ID to determine the plan
                        var lineItemService = new SessionLineItemService();
                        var lineItems = await lineItemService.ListAsync(session.Id);
                        var purchasedPriceId = lineItems.Data.FirstOrDefault()?.Price?.Id ?? "";

                        if (purchasedPriceId == teamPriceId)
                        {
                            organization.SubscriptionTier = SubscriptionTier.Enterprise;
                            organization.MaxAdmins = 10;
                            if (organization.AccountType == OrgAccountType.Individual)
                                organization.AccountType = OrgAccountType.Corporate;
                        }
                        else if (purchasedPriceId == proPriceId)
                        {
                            organization.SubscriptionTier = SubscriptionTier.Pro;
                            organization.MaxAdmins = 5;
                        }
                        else
                        {
                            // Fallback: upgrade to Pro
                            organization.SubscriptionTier = SubscriptionTier.Pro;
                            organization.MaxAdmins = 5;
                        }

                        organization.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Upgraded org {OrgId} to tier {Tier} (MaxAdmins={MaxAdmins}).",
                            orgId, organization.SubscriptionTier, organization.MaxAdmins);
                    }
                }
            }
            else if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var subscription = stripeEvent.Data.Object as Subscription;
                if (subscription?.Metadata?.TryGetValue("orgId", out var orgIdStr) == true
                    && Guid.TryParse(orgIdStr, out Guid downgradeOrgId))
                {
                    var organization = await _context.Organizations.FindAsync(downgradeOrgId);
                    if (organization != null)
                    {
                        organization.SubscriptionTier = SubscriptionTier.Free;
                        organization.MaxAdmins = 3;
                        organization.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Downgraded org {OrgId} to Free tier.", downgradeOrgId);
                    }
                }
            }

            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled exception in webhook");
            return StatusCode(500);
        }
    }

    public class CheckoutRequest
    {
        public string PlanId { get; set; } = string.Empty;
    }
}
