namespace LinkVaultApi.Services;

public interface IFileService
{
    /// <summary>Saves an uploaded file to disk and returns the relative URL path.</summary>
    Task<string> SaveFileAsync(IFormFile file, Guid userId);

    /// <summary>Deletes a file from storage by its relative URL path.</summary>
    Task DeleteFileAsync(string relativePath);

    /// <summary>Returns allowed MIME types for upload.</summary>
    IReadOnlyList<string> AllowedMimeTypes { get; }

    /// <summary>Maximum file size in bytes (default 10MB).</summary>
    long MaxFileSizeBytes { get; }
}
