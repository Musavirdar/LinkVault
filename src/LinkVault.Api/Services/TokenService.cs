using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace LinkVault.Api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new("account_type", user.AccountType.ToString())
        };

        if (user.OrganizationId.HasValue)
            claims.Add(new Claim("organization_id", user.OrganizationId.Value.ToString()));

        foreach (var userRole in user.UserRoles)
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role?.Name ?? string.Empty));

        return BuildToken(claims, GetTokenExpiry());
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string Generate2FAToken(Guid userId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("purpose", "2fa")
        };
        return BuildToken(claims, TimeSpan.FromMinutes(10));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch { return null; }
    }

    private string BuildToken(IEnumerable<Claim> claims, TimeSpan expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TimeSpan GetTokenExpiry() =>
        TimeSpan.FromMinutes(int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60"));
}
