using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LinkVaultApi.Controllers;
using LinkVaultApi.Data;
using LinkVaultApi.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace LinkVaultApi.Tests.Integration;

public class ChunkedUploadValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ChunkedUploadValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteUpload_WithDisguisedExecutablePayload_ShouldReturn400_AndPurgeChunks()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId, orgId, role: "Member");

        var storageChunks = new ConcurrentDictionary<string, byte[]>();
        _factory.MockStorageProvider
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, string, CancellationToken>(async (stream, path, ct) =>
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                storageChunks[path] = ms.ToArray();
                return path;
            });

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((path, ct) =>
            {
                if (storageChunks.TryGetValue(path, out var bytes))
                {
                    return Task.FromResult<Stream>(new MemoryStream(bytes));
                }
                return Task.FromResult<Stream>(new MemoryStream());
            });

        _factory.MockStorageProvider
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((path, ct) =>
            {
                storageChunks.TryRemove(path, out _);
                return Task.CompletedTask;
            });

        // 1. Init upload
        var initRequest = new InitUploadRequest
        {
            FileName = "malicious.pdf",
            MimeType = "application/pdf",
            TotalSize = 8,
            TotalChunks = 1,
            ChunkSize = 1024
        };

        var initResponse = await client.PostAsJsonAsync("/api/upload/init", initRequest);
        initResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initJson = await initResponse.Content.ReadAsStringAsync();
        using var initDoc = JsonDocument.Parse(initJson);
        var uploadId = initDoc.RootElement.GetProperty("uploadId").GetString()!;

        // 2. Upload chunk containing DOS executable magic bytes (MZ header)
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(exeBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "chunk", "chunk.bin");

        var chunkResponse = await client.PutAsync($"/api/upload/{uploadId}/chunk/0", form);
        chunkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Complete upload
        var completeResponse = await client.PostAsync($"/api/upload/{uploadId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorBody = await completeResponse.Content.ReadAsStringAsync();
        errorBody.Should().Contain("Executable files");

        // Verify chunks were purged from storage
        storageChunks.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteUpload_WithValidPdfPayload_ShouldReturn200_AndPersistResource()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId, orgId, role: "Member");

        var storageChunks = new ConcurrentDictionary<string, byte[]>();
        _factory.MockStorageProvider
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, string, CancellationToken>(async (stream, path, ct) =>
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                storageChunks[path] = ms.ToArray();
                return path;
            });

        _factory.MockStorageProvider
            .Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((path, ct) =>
            {
                if (storageChunks.TryGetValue(path, out var bytes))
                {
                    return Task.FromResult<Stream>(new MemoryStream(bytes));
                }
                return Task.FromResult<Stream>(new MemoryStream());
            });

        _factory.MockStorageProvider
            .Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((path, ct) =>
            {
                storageChunks.TryRemove(path, out _);
                return Task.CompletedTask;
            });

        // 1. Init upload
        var initRequest = new InitUploadRequest
        {
            FileName = "valid.pdf",
            MimeType = "application/pdf",
            TotalSize = 25,
            TotalChunks = 1,
            ChunkSize = 1024
        };

        var initResponse = await client.PostAsJsonAsync("/api/upload/init", initRequest);
        initResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var initJson = await initResponse.Content.ReadAsStringAsync();
        using var initDoc = JsonDocument.Parse(initJson);
        var uploadId = initDoc.RootElement.GetProperty("uploadId").GetString()!;

        // 2. Upload valid PDF chunk
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%âãÏÓ\n1 0 obj...");
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "chunk", "chunk.bin");

        var chunkResponse = await client.PutAsync($"/api/upload/{uploadId}/chunk/0", form);
        chunkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Complete upload
        var completeResponse = await client.PostAsync($"/api/upload/{uploadId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseJson = await completeResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var resourceId = doc.RootElement.GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LinkVaultDbContext>();
        var resource = await db.Resources.FindAsync(resourceId);
        resource.Should().NotBeNull();
        resource!.Name.Should().Be("valid.pdf");
        resource.OrganizationId.Should().Be(orgId);
    }
}
