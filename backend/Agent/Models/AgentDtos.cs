using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkVaultApi.Agent.Models;

public class CreatePlanRequest
{
    [Required]
    [MaxLength(1000)]
    public string UserGoal { get; set; } = string.Empty;

    public string? PreferredModel { get; set; }
}

public class PlanStepDto
{
    public int StepNumber { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AgentPlanResponse
{
    public Guid PlanId { get; set; }
    public string UserGoal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<PlanStepDto> Steps { get; set; } = new();
    public bool RequiresUserApproval { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApprovePlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Executing";
    public string Message { get; set; } = string.Empty;
}

public class RejectPlanResponse
{
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "Rejected";
    public string Message { get; set; } = string.Empty;
}

public class SafeShareScanRequest
{
    [Required]
    public Guid ResourceId { get; set; }
}

public class SafeShareScanResponse
{
    public Guid ResourceId { get; set; }
    public string? FileName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public List<string> DetectedThreats { get; set; } = new();
    public bool CanShare { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
