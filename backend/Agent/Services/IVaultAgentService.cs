using System.Security.Claims;
using LinkVaultApi.Agent.Models;

namespace LinkVaultApi.Agent.Services;

public interface IVaultAgentService
{
    Task<AgentPlanResponse> GeneratePlanAsync(CreatePlanRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ApprovePlanResponse> ApprovePlanAsync(Guid planId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<RejectPlanResponse> RejectPlanAsync(Guid planId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<AgentPlanResponse?> GetPlanAsync(Guid planId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
