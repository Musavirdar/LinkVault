using System.Net;
using FluentAssertions;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinkVaultApi.Tests.Integration;

public class AuditControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuditLogs_AsStandardMember_ShouldReturn403Forbidden()
    {
        var orgId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        var client = _factory.CreateAuthenticatedClient(memberUserId, orgId, role: "Member");
        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditSummary_AsStandardMember_ShouldReturn403Forbidden()
    {
        var orgId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        var client = _factory.CreateAuthenticatedClient(memberUserId, orgId, role: "Member");
        var response = await client.GetAsync("/api/audit/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRecentAudit_AsStandardMember_ShouldReturn403Forbidden()
    {
        var orgId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();

        var client = _factory.CreateAuthenticatedClient(memberUserId, orgId, role: "Member");
        var response = await client.GetAsync("/api/audit/recent");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_AsAdmin_ShouldReturn200_AndIsolateTenantLogs()
    {
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();
        var admin1Id = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.AuditLogs.AddRange(
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org1Id,
                    UserId = admin1Id,
                    ActionType = AuditActionType.Created,
                    EntityType = ResourceEntityType.Resource,
                    Description = "Org 1 sensitive log",
                    CreatedAt = DateTime.UtcNow
                },
                new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org2Id,
                    UserId = Guid.NewGuid(),
                    ActionType = AuditActionType.Created,
                    EntityType = ResourceEntityType.Resource,
                    Description = "Org 2 secret log",
                    CreatedAt = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(admin1Id, org1Id, role: "Admin");
        var response = await client.GetAsync("/api/audit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Org 1 sensitive log");
        content.Should().NotContain("Org 2 secret log");
    }
}
