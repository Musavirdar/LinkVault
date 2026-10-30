using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinkVaultApi.Agent.Hubs;

[Authorize]
public class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinPlanGroup(string planId)
    {
        var groupName = $"agentPlan_{planId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} subscribed to agent plan group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeavePlanGroup(string planId)
    {
        var groupName = $"agentPlan_{planId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} unsubscribed from agent plan group {GroupName}", Context.ConnectionId, groupName);
    }

    public static string GetGroupName(Guid planId) => $"agentPlan_{planId}";
}
