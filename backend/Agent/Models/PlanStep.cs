using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkVaultApi.Agent.Models;

public enum StepStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}

public class PlanStep
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    public int StepNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public string ToolName { get; set; } = string.Empty;

    [Required]
    public string ParametersJson { get; set; } = "{}";

    [Required]
    public bool RequiresApproval { get; set; } = true;

    [Required]
    public StepStatus Status { get; set; } = StepStatus.Pending;

    [MaxLength(500)]
    public string? Description { get; set; }

    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    [ForeignKey("PlanId")]
    public virtual AgentActionPlan Plan { get; set; } = null!;
}
