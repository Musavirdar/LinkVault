using LinkVault.Api.Models.DTOs.Common;

namespace LinkVault.Api.Services.Interfaces;

public interface IFileService
{
    Task<FileDto> UploadAsync(IFormFile file, Guid userId, Guid directoryId);
    Task<FileDto> GetByIdAsync(Guid userId, Guid fileId);
    Task<PagedResult<FileDto>> GetDirectoryFilesAsync(Guid userId, Guid directoryId, int page, int pageSize);
    Task DeleteAsync(Guid userId, Guid fileId);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid userId, Guid fileId);
    Task<string> CreateShareLinkAsync(Guid userId, Guid fileId, DateTime? expiresAt, string? password, int? maxDownloads);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadViaShareLinkAsync(string token, string? password);
}

public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? StorageProvider { get; set; }
    public int ViewCount { get; set; }
    public int DownloadCount { get; set; }
    public Guid DirectoryId { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime CreatedAt { get; set; }
}
