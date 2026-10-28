using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace LinkVaultApi.Tests.Integration;

public class DownloadEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DownloadEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StaticUploadsFolder_ShouldReturn404_WhenRequestedDirectly()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/uploads/testuser/secret_file.pdf");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadFile_WithoutAuthOrTicket_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var fileId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/files/{fileId}/download");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadFile_WithCrossOrgToken_ShouldReturn403Or404()
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
                Name = "org1_secret.pdf",
                OrganizationId = org1Id,
                CreatedById = Guid.NewGuid(),
                MimeType = "application/pdf",
                StoragePath = "org1/secret.pdf",
                SizeInBytes = 1024,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateAuthenticatedClient(user2Id, org2Id, role: "Member");
        var response = await client.GetAsync($"/api/files/{resourceId}/download");

        // Tenant isolation: forbidden or not found
        response.StatusCode.Should().Match(s => s == HttpStatusCode.Forbidden || s == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadFile_WithValidBearerAuth_ShouldStreamFile()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var fileContent = "Hello from LinkVault secure file storage!";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "contract.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/contract.pdf",
                SizeInBytes = fileBytes.Length,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var client = _factory.CreateAuthenticatedClient(userId, orgId, role: "Member");
        var response = await client.GetAsync($"/api/files/{resourceId}/download");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(fileContent);
    }

    [Fact]
    public async Task DownloadTicket_Flow_ShouldWorkOnce_AndRejectReplay()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var fileContent = "Secure ticket stream content";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "report.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/report.pdf",
                SizeInBytes = fileBytes.Length,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var authClient = _factory.CreateAuthenticatedClient(userId, orgId);

        // 1. Create ticket
        var ticketResponse = await authClient.PostAsync($"/api/files/{resourceId}/ticket", null);
        ticketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketJson = await ticketResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(ticketJson);
        var ticket = doc.RootElement.GetProperty("ticket").GetString();
        ticket.Should().NotBeNullOrWhiteSpace();

        // 2. Consume ticket via unauthenticated browser GET with ?ticket=
        var anonClient = _factory.CreateClient();
        var downloadResponse = await anonClient.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await downloadResponse.Content.ReadAsStringAsync();
        content.Should().Be(fileContent);

        // 3. Second consumption must be rejected (single-use)
        var replayResponse = await anonClient.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");
        replayResponse.StatusCode.Should().Match(s => s == HttpStatusCode.Unauthorized || s == HttpStatusCode.Forbidden || s == HttpStatusCode.Gone);
    }
}
