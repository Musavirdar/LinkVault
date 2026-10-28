using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LinkVaultApi.Hubs;

[Authorize]
public class ActivityHub : Hub
{
    private readonly IAuditService _auditService;
    private readonly ILogger<ActivityHub> _logger;

    public ActivityHub(IAuditService auditService, ILogger<ActivityHub> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var orgId = GetOrganizationId();
        
        if (orgId.HasValue)
        {
            // Join organization group for activity updates
            await Groups.AddToGroupAsync(Context.ConnectionId, orgId.Value.ToString());
            
            _logger.LogInformation("User {UserId} joined activity feed for org {OrgId}", userId, orgId.Value);
            
            // Send recent activity
            var recentActivity = await _auditService.GetRecentActivityAsync(orgId.Value, 20);
            await Clients.Caller.SendAsync("RecentActivity", recentActivity);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var orgId = GetOrganizationId();
        
        if (orgId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, orgId.Value.ToString());
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToDirectory(string directoryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dir:{directoryId}");
        _logger.LogInformation("User subscribed to directory {DirectoryId} activity", directoryId);
    }

    public async Task UnsubscribeFromDirectory(string directoryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dir:{directoryId}");
        _logger.LogInformation("User unsubscribed from directory {DirectoryId} activity", directoryId);
    }

    // Called by server to broadcast activity
    public static async Task BroadcastActivity(IHubContext<ActivityHub> hubContext, Guid organizationId, object activity)
    {
        await hubContext.Clients.Group(organizationId.ToString()).SendAsync("NewActivity", activity);
    }

    // Called by server to broadcast directory-specific activity
    public static async Task BroadcastDirectoryActivity(IHubContext<ActivityHub> hubContext, Guid directoryId, object activity)
    {
        await hubContext.Clients.Group($"dir:{directoryId}").SendAsync("NewDirectoryActivity", activity);
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        throw new HubException("User not authenticated");
    }

    private Guid? GetOrganizationId()
    {
        var orgIdClaim = Context.User?.FindFirst("orgId");
        if (orgIdClaim != null && Guid.TryParse(orgIdClaim.Value, out var orgId))
        {
            return orgId;
        }
        return null;
    }
}
