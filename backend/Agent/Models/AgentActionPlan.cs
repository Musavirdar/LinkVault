using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LinkVaultApi.Models;

namespace LinkVaultApi.Agent.Models;

public enum PlanStatus
{
    Draft = 0,
    AwaitingApproval = 1,
    Executing = 2,
    Completed = 3,
    Rejected = 4,
    Failed = 5
}

public class AgentActionPlan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string UserGoal { get; set; } = string.Empty;

    [Required]
    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<PlanStep> Steps { get; set; } = new List<PlanStep>();
}
