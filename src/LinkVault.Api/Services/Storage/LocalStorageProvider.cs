using LinkVault.Api.Services.Interfaces;

namespace LinkVault.Api.Services.Storage;

/// <summary>
/// Default storage provider — saves files under /uploads in the content root.
/// Swap out by registering a different IStorageProvider in DI (S3, Azure Blob, etc).
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly IWebHostEnvironment _env;

    public LocalStorageProvider(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string GetFullPath(string storagePath) => storagePath;

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType)
    {
        var uploadDir = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadDir, uniqueName);

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs);

        return fullPath; // storagePath = full path for local provider
    }

    public Task<Stream> OpenReadAsync(string storagePath)
    {
        if (!File.Exists(storagePath))
            throw new FileNotFoundException("File not found on local storage", storagePath);

        return Task.FromResult<Stream>(File.OpenRead(storagePath));
    }

    public Task DeleteAsync(string storagePath)
    {
        if (File.Exists(storagePath))
            File.Delete(storagePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath) =>
        Task.FromResult(File.Exists(storagePath));
}
