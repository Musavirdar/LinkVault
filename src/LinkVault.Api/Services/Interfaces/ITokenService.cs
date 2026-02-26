using System.Security.Claims;
using LinkVault.Api.Models.Entities;

namespace LinkVault.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string Generate2FAToken(Guid userId);
    ClaimsPrincipal? ValidateToken(string token);
}
