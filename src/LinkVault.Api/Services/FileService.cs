using LinkVault.Api.Data;
using LinkVault.Api.Exceptions;
using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.Entities;
using LinkVault.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Services;

/// <summary>
/// Local file storage implementation. Replace StoragePath logic
/// with S3/Azure calls when integrating cloud storage.
/// </summary>
public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditService _audit;

    public FileService(ApplicationDbContext context, IWebHostEnvironment env, IAuditService audit)
    {
        _context = context;
        _env = env;
        _audit = audit;
    }

    public async Task<FileDto> UploadAsync(IFormFile file, Guid userId, Guid directoryId)
    {
        var dir = await _context.Directories.FindAsync(directoryId)
            ?? throw new NotFoundException("Directory not found");

        if (dir.OwnerId != userId)
            throw new UnauthorizedException("Access denied to directory");

        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(_env.ContentRootPath, "uploads", storedName);

        Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);
        await using (var stream = File.Create(uploadPath))
            await file.CopyToAsync(stream);

        var entity = new FileItem
        {
            Id = Guid.NewGuid(),
            FileName = storedName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = uploadPath,
            StorageProvider = "Local",
            DirectoryId = directoryId,
            UploadedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Files.Add(entity);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(userId, AuditAction.Upload, "File", entity.Id, file.FileName);

        return MapToDto(entity);
    }

    public async Task<FileDto> GetByIdAsync(Guid userId, Guid fileId)
    {
        var file = await GetAndAuthorizeAsync(userId, fileId);
        file.ViewCount++;
        await _context.SaveChangesAsync();
        return MapToDto(file);
    }

    public async Task<PagedResult<FileDto>> GetDirectoryFilesAsync(Guid userId, Guid directoryId, int page, int pageSize)
    {
        var query = _context.Files
            .Where(f => f.DirectoryId == directoryId && !f.IsDeleted);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<FileDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task DeleteAsync(Guid userId, Guid fileId)
    {
        var file = await GetAndAuthorizeAsync(userId, fileId);
        file.IsDeleted = true;
        file.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(userId, AuditAction.Delete, "File", file.Id, file.OriginalFileName);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid userId, Guid fileId)
    {
        var file = await GetAndAuthorizeAsync(userId, fileId);

        if (!File.Exists(file.StoragePath))
            throw new NotFoundException("File not found on storage");

        file.DownloadCount++;
        await _context.SaveChangesAsync();
        await _audit.LogAsync(userId, AuditAction.Download, "File", file.Id, file.OriginalFileName);

        return (File.OpenRead(file.StoragePath), file.ContentType, file.OriginalFileName);
    }

    public async Task<string> CreateShareLinkAsync(Guid userId, Guid fileId, DateTime? expiresAt, string? password, int? maxDownloads)
    {
        await GetAndAuthorizeAsync(userId, fileId);
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var shareLink = new ShareLink
        {
            Id = Guid.NewGuid(),
            Token = token,
            FileId = fileId,
            CreatedById = userId,
            ExpiresAt = expiresAt,
            MaxDownloads = maxDownloads,
            Password = password != null ? BCrypt.Net.BCrypt.HashPassword(password) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShareLinks.Add(shareLink);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadViaShareLinkAsync(string token, string? password)
    {
        var shareLink = await _context.ShareLinks
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive)
            ?? throw new NotFoundException("Share link not found or expired");

        if (shareLink.ExpiresAt.HasValue && shareLink.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Share link has expired");

        if (shareLink.MaxDownloads.HasValue && shareLink.DownloadCount >= shareLink.MaxDownloads)
            throw new UnauthorizedException("Download limit reached");

        if (shareLink.Password != null)
        {
            if (password == null || !BCrypt.Net.BCrypt.Verify(password, shareLink.Password))
                throw new UnauthorizedException("Invalid password");
        }

        shareLink.DownloadCount++;
        await _context.SaveChangesAsync();

        var file = shareLink.File;
        if (!File.Exists(file.StoragePath))
            throw new NotFoundException("File not found on storage");

        return (File.OpenRead(file.StoragePath), file.ContentType, file.OriginalFileName);
    }

    private async Task<FileItem> GetAndAuthorizeAsync(Guid userId, Guid fileId)
    {
        var file = await _context.Files
            .Include(f => f.Directory)
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted)
            ?? throw new NotFoundException("File not found");

        if (file.UploadedById != userId && file.Directory?.OwnerId != userId)
            throw new UnauthorizedException("Access denied");

        return file;
    }

    private static FileDto MapToDto(FileItem f) => new()
    {
        Id = f.Id,
        FileName = f.FileName,
        OriginalFileName = f.OriginalFileName,
        ContentType = f.ContentType,
        FileSize = f.FileSize,
        StorageProvider = f.StorageProvider,
        ViewCount = f.ViewCount,
        DownloadCount = f.DownloadCount,
        DirectoryId = f.DirectoryId,
        UploadedById = f.UploadedById,
        CreatedAt = f.CreatedAt
    };
}
