using System.Security.Claims;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

/// <summary>
/// TeraTransfer-style quick file sharing.
/// Anonymous users: 5 transfers/hour, max 500MB total.
/// Authenticated users: 50 transfers/hour, configurable limits.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly LinkVaultDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly ILogger<TransferController> _logger;

    public TransferController(LinkVaultDbContext db, IStorageProvider storage,
        ILogger<TransferController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>Upload files and create a temporary transfer link.</summary>
    [HttpPost]
    [AllowAnonymous]
    [RequestSizeLimit(500L * 1024 * 1024)] // 500MB max per transfer
    public async Task<ActionResult> CreateTransfer([FromForm] List<IFormFile> files, [FromForm] string? title)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No files provided." });

        var userId = TryGetUserId();

        var transfer = new Transfer
        {
            Title = title ?? $"Transfer ({files.Count} file{(files.Count > 1 ? "s" : "")})",
            CreatedByUserId = userId,
            FileCount = files.Count,
            TotalSizeBytes = files.Sum(f => f.Length)
        };

        _db.Transfers.Add(transfer);

        foreach (var file in files)
        {
            var safeName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace("..", "").Replace("/", "").Replace("\\", "");
            var ext = Path.GetExtension(file.FileName);
            var storagePath = $"_transfers/{transfer.Token}/{safeName}_{Guid.NewGuid():N}{ext}";

            await using var stream = file.OpenReadStream();
            await _storage.SaveAsync(stream, storagePath);

            var transferFile = new TransferFile
            {
                TransferId = transfer.Id,
                FileName = file.FileName,
                MimeType = file.ContentType,
                SizeInBytes = file.Length,
                StoragePath = storagePath
            };

            _db.TransferFiles.Add(transferFile);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Transfer {Token} created: {Count} files, {Size} bytes",
            transfer.Token, files.Count, transfer.TotalSizeBytes);

        return Ok(new
        {
            token = transfer.Token,
            url = $"/t/{transfer.Token}",
            title = transfer.Title,
            fileCount = transfer.FileCount,
            totalSize = FormatSize(transfer.TotalSizeBytes),
            expiresAt = transfer.ExpiresAt
        });
    }

    /// <summary>Get transfer info (public).</summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTransfer(string token)
    {
        var transfer = await _db.Transfers
            .Include(t => t.Files)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

        if (transfer == null)
            return NotFound(new { error = "Transfer not found." });

        if (transfer.ExpiresAt < DateTime.UtcNow)
            return StatusCode(410, new { error = "This transfer has expired." });

        if (transfer.MaxDownloads.HasValue && transfer.DownloadCount >= transfer.MaxDownloads.Value)
            return StatusCode(410, new { error = "Download limit reached." });

        return Ok(new
        {
            token = transfer.Token,
            title = transfer.Title,
            fileCount = transfer.FileCount,
            totalSize = FormatSize(transfer.TotalSizeBytes),
            totalSizeBytes = transfer.TotalSizeBytes,
            downloadCount = transfer.DownloadCount,
            expiresAt = transfer.ExpiresAt,
            createdAt = transfer.CreatedAt,
            createdBy = transfer.CreatedBy?.DisplayName ?? transfer.CreatedBy?.Username ?? "Anonymous",
            files = transfer.Files.Select(f => new
            {
                id = f.Id,
                fileName = f.FileName,
                mimeType = f.MimeType,
                size = FormatSize(f.SizeInBytes),
                sizeBytes = f.SizeInBytes
            })
        });
    }

    /// <summary>Download a specific file from a transfer.</summary>
    [HttpGet("{token}/download/{fileId}")]
    [AllowAnonymous]
    public async Task<ActionResult> DownloadFile(string token, Guid fileId)
    {
        var transfer = await _db.Transfers
            .Include(t => t.Files)
            .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

        if (transfer == null) return NotFound(new { error = "Transfer not found." });
        if (transfer.ExpiresAt < DateTime.UtcNow)
            return StatusCode(410, new { error = "Transfer expired." });

        var file = transfer.Files.FirstOrDefault(f => f.Id == fileId);
        if (file == null) return NotFound(new { error = "File not found in transfer." });

        transfer.DownloadCount++;
        await _db.SaveChangesAsync();

        try
        {
            var stream = await _storage.GetAsync(file.StoragePath);
            return File(stream, file.MimeType ?? "application/octet-stream", file.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "File no longer available." });
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        int i = 0; double s = bytes;
        while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
        return $"{s:0.##} {units[i]}";
    }

    private Guid? TryGetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
