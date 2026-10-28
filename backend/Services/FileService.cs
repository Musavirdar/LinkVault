namespace LinkVaultApi.Services;

public class FileService : IFileService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly string[] _allowedMimeTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "application/pdf",
        "text/plain", "text/csv", "text/markdown",
        "application/json",
        "application/zip", "application/x-zip-compressed",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "video/mp4", "audio/mpeg"
    ];

    public IReadOnlyList<string> AllowedMimeTypes => _allowedMimeTypes;

    public long MaxFileSizeBytes => _config.GetValue<long>("FileStorage:MaxFileSizeBytes", 10 * 1024 * 1024); // 10MB

    public FileService(IConfiguration config, IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file, Guid userId)
    {
        var uploadsRoot = _config.GetValue<string>("FileStorage:UploadsPath") 
            ?? Path.Combine(_env.ContentRootPath, "uploads");

        var userFolder = Path.Combine(uploadsRoot, userId.ToString());
        Directory.CreateDirectory(userFolder);

        var safeFileName = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");
        var ext = Path.GetExtension(file.FileName);
        var uniqueName = $"{safeFileName}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(userFolder, uniqueName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("Saved file {FileName} for user {UserId}", uniqueName, userId);

        // Return relative URL path served via static files
        return $"/uploads/{userId}/{uniqueName}";
    }

    public Task DeleteFileAsync(string relativePath)
    {
        var uploadsRoot = _config.GetValue<string>("FileStorage:UploadsPath")
            ?? Path.Combine(_env.ContentRootPath, "uploads");

        // relativePath is like /uploads/{userId}/{filename}
        var diskPath = Path.Combine(_env.ContentRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(diskPath))
        {
            File.Delete(diskPath);
            _logger.LogInformation("Deleted file at {Path}", diskPath);
        }

        return Task.CompletedTask;
    }
}
