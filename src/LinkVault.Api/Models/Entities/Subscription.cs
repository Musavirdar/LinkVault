namespace LinkVault.Api.Models.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public SubscriptionTier Tier { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime TrialEndsAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Belongs to either a user (Individual) or an org (Corporate)
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }

    // Navigation
    public User? User { get; set; }
    public Organization? Organization { get; set; }
}

public enum SubscriptionTier
{
    IndividualFree = 0,
    IndividualPro = 1,
    CorporateBasic = 2,
    CorporateEnterprise = 3
}

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    PastDue = 2,
    Cancelled = 3,
    Expired = 4
}
