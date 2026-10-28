namespace LinkVaultApi.Services;

/// <summary>
/// Local filesystem storage provider. Stores files under the configured uploads directory.
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration config, IWebHostEnvironment env, ILogger<LocalStorageProvider> logger)
    {
        _basePath = config.GetValue<string>("FileStorage:UploadsPath")
            ?? Path.Combine(env.ContentRootPath, "uploads");
        Directory.CreateDirectory(_basePath);
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream stream, string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await stream.CopyToAsync(fs, ct);

        _logger.LogInformation("Saved file to {Path} ({Size} bytes)", path, fs.Length);
        return path;
    }

    public Task<Stream> GetAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file at {Path}", path);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<long> GetSizeAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var info = new FileInfo(fullPath);
        return Task.FromResult(info.Exists ? info.Length : 0L);
    }
}
