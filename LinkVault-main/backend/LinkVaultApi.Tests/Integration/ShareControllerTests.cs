using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LinkVaultApi.Controllers;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LinkVaultApi.Tests.Integration;

public class ShareControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ShareControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateShareLink_WithoutAuthentication_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var request = new CreateShareRequest
        {
            ResourceId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/share", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateShareLink_WithoutCanSharePolicy_ShouldReturn403()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        // Create user client without CanShare policy (e.g. Viewer role)
        var client = _factory.CreateAuthenticatedClient(userId, orgId, role: "Viewer");
        var request = new CreateShareRequest
        {
            ResourceId = resourceId
        };

        var response = await client.PostAsJsonAsync("/api/share", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateShareLink_ForCrossTenantResource_ShouldReturn404()
    {
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "org1_file.pdf",
                OrganizationId = org1Id, // Org 1 owns the resource
                CreatedById = Guid.NewGuid(),
                MimeType = "application/pdf",
                StoragePath = "docs/file.pdf",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // User from Org 2 attempts to create share link for Org 1's resource
        var client = _factory.CreateAuthenticatedClient(user2Id, org2Id, role: "Member");
        var request = new CreateShareRequest
        {
            ResourceId = resourceId
        };

        var response = await client.PostAsJsonAsync("/api/share", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateShareLink_WithValidOrgAndPolicy_ShouldReturn200_AndRecordOrgId()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "company_handbook.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/handbook.pdf",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(userId, orgId, role: "Member");
        var request = new CreateShareRequest
        {
            ResourceId = resourceId,
            AllowPreview = true
        };

        var response = await client.PostAsJsonAsync("/api/share", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        // Verify share link in database has OrganizationId recorded
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            var savedShare = db.ShareLinks.FirstOrDefault(s => s.Token == token);
            savedShare.Should().NotBeNull();
            savedShare!.OrganizationId.Should().Be(orgId);
        }
    }
}
