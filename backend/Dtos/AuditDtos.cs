using System.ComponentModel.DataAnnotations;
using LinkVaultApi.Models;

namespace LinkVaultApi.Dtos;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public bool IsViaAgent { get; set; }
    public string? AgentToolName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterRequestDto
{
    public AuditActionType? ActionType { get; set; }
    public ResourceEntityType? EntityType { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EntityId { get; set; }
    public bool? IsViaAgent { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AuditLogListResponseDto
{
    public List<AuditLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AuditSummaryDto
{
    public int TotalActionsToday { get; set; }
    public int TotalActionsThisWeek { get; set; }
    public int TotalActionsThisMonth { get; set; }
    public int TotalAgentActions { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public Dictionary<string, int> ActionsByUser { get; set; } = new();
}
