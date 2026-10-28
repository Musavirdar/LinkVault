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

    [Fact]
    public async Task DownloadTicket_ConcurrentRedemption_ExactlyOneSucceeds()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var fileContent = "Concurrent ticket test stream";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "concurrent_test.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/concurrent_test.pdf",
                SizeInBytes = fileBytes.Length,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var authClient = _factory.CreateAuthenticatedClient(userId, orgId);

        // Issue ticket
        var ticketResponse = await authClient.PostAsync($"/api/files/{resourceId}/ticket", null);
        ticketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketJson = await ticketResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(ticketJson);
        var ticket = doc.RootElement.GetProperty("ticket").GetString();
        ticket.Should().NotBeNullOrWhiteSpace();

        // Fire 2 concurrent unauthenticated requests using the same ticket
        var anonClient1 = _factory.CreateClient();
        var anonClient2 = _factory.CreateClient();

        var task1 = anonClient1.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");
        var task2 = anonClient2.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");

        var responses = await Task.WhenAll(task1, task2);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rejectedCount = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized || r.StatusCode == HttpStatusCode.Gone);

        successCount.Should().Be(1, "exactly one concurrent request must claim the ticket");
        rejectedCount.Should().Be(1, "all subsequent concurrent requests must be rejected");
    }

    [Fact]
    public async Task DownloadTicket_SoftDeletedOrInactiveResource_ReturnsNotFound()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var fileContent = "Deleted resource test";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "deleted_resource.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/deleted_resource.pdf",
                SizeInBytes = fileBytes.Length,
                IsActive = true,
                IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var authClient = _factory.CreateAuthenticatedClient(userId, orgId);

        // Issue ticket
        var ticketResponse = await authClient.PostAsync($"/api/files/{resourceId}/ticket", null);
        ticketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketJson = await ticketResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(ticketJson);
        var ticket = doc.RootElement.GetProperty("ticket").GetString();

        // Soft-delete resource after ticket issuance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            var res = await db.Resources.FindAsync(resourceId);
            res!.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var anonClient = _factory.CreateClient();
        var downloadResponse = await anonClient.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadTicket_InactiveUser_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var fileContent = "Inactive user test";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            db.Users.Add(new User
            {
                Id = userId,
                Email = "creator@example.com",
                Username = "creator",
                OrganizationId = orgId,
                IsActive = true
            });
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "user_test.pdf",
                OrganizationId = orgId,
                CreatedById = userId,
                MimeType = "application/pdf",
                StoragePath = "docs/user_test.pdf",
                SizeInBytes = fileBytes.Length,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));

        var authClient = _factory.CreateAuthenticatedClient(userId, orgId);

        // Issue ticket
        var ticketResponse = await authClient.PostAsync($"/api/files/{resourceId}/ticket", null);
        ticketResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticketJson = await ticketResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(ticketJson);
        var ticket = doc.RootElement.GetProperty("ticket").GetString();

        // Deactivate user after ticket issuance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
            var u = await db.Users.FindAsync(userId);
            u!.IsActive = false;
            await db.SaveChangesAsync();
        }

        var anonClient = _factory.CreateClient();
        var downloadResponse = await anonClient.GetAsync($"/api/files/{resourceId}/download?ticket={ticket}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
