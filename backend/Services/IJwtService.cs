using System.Security.Claims;
using LinkVaultApi.Models;

namespace LinkVaultApi.Services;

public interface IJwtService
{
    string GenerateToken(User user, Role? role, IEnumerable<Claim>? additionalClaims = null);
    string GeneratePreAuthToken(Guid userId);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    ClaimsPrincipal? ValidatePreAuthToken(string token);
    DateTime GetTokenExpiry();
}
