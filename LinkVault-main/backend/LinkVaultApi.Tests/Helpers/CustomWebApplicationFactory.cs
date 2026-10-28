using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace LinkVaultApi.Tests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"LinkVaultTestDb_{Guid.NewGuid()}";
    public Mock<IStorageProvider> MockStorageProvider { get; } = new();

    public const string TestJwtSecret = "lv-dev-secret-key-a8f3b9c2e1d4a7f6b5c8e2d1f4a9b3c7";
    public const string TestIssuer = "LinkVaultApi";
    public const string TestAudience = "LinkVaultClient";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = TestJwtSecret,
                ["JwtSettings:Issuer"] = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=linkvault_test;Username=postgres;Password=postgres"
            };
            config.AddInMemoryCollection(inMemoryConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Reconfigure JWT validation to ensure it uses the test secret and parameters
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Remove existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LinkVaultDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Register InMemory database for tests
            services.AddDbContext<LinkVaultDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // Replace IStorageProvider with MockStorageProvider
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageProvider));
            if (storageDescriptor != null)
            {
                services.Remove(storageDescriptor);
            }
            services.AddSingleton(MockStorageProvider.Object);
        });
    }

    public string GenerateJwtToken(Guid userId, Guid orgId, string role = "Member", IEnumerable<string>? claims = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtSecret);

        var claimsList = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new("orgId", orgId.ToString()),
            new("sub", userId.ToString()),
            new("accountType", "Corporate")
        };

        if (role == "Admin")
        {
            claimsList.Add(new Claim("canUpload", "True"));
            claimsList.Add(new Claim("canUpdate", "True"));
            claimsList.Add(new Claim("canDelete", "True"));
            claimsList.Add(new Claim("canExport", "True"));
            claimsList.Add(new Claim("canShare", "True"));
            claimsList.Add(new Claim("canManageUsers", "True"));
            claimsList.Add(new Claim("canManageRoles", "True"));
            claimsList.Add(new Claim("canManageSettings", "True"));
            claimsList.Add(new Claim("canViewAuditLogs", "True"));
        }
        else if (role == "Manager")
        {
            claimsList.Add(new Claim("canUpload", "True"));
            claimsList.Add(new Claim("canUpdate", "True"));
            claimsList.Add(new Claim("canDelete", "True"));
            claimsList.Add(new Claim("canExport", "True"));
            claimsList.Add(new Claim("canShare", "True"));
            claimsList.Add(new Claim("canManageUsers", "True"));
            claimsList.Add(new Claim("canViewAuditLogs", "True"));
        }
        else if (role == "Member")
        {
            claimsList.Add(new Claim("canUpload", "True"));
            claimsList.Add(new Claim("canUpdate", "True"));
            claimsList.Add(new Claim("canExport", "True"));
            claimsList.Add(new Claim("canShare", "True"));
        }

        if (claims != null)
        {
            foreach (var customClaim in claims)
            {
                claimsList.Add(new Claim(customClaim, "True"));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsList),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, Guid orgId, string role = "Member", IEnumerable<string>? claims = null)
    {
        var client = CreateClient();
        var token = GenerateJwtToken(userId, orgId, role, claims);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
