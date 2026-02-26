using System.Security.Claims;

namespace LinkVault.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in claims.");
        return Guid.Parse(claim.Value);
    }

    public static string GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public static string GetUsername(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
}
