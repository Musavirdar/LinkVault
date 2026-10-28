using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

/// <summary>
/// Terabox-style chunked upload controller.
/// Flow: POST /init → PUT /chunk/{index} → POST /complete
/// Supports resume on failure and parallel chunk uploads.
/// </summary>
[ApiController]
[Route("api/upload")]
[Authorize]
public class ChunkedUploadController : ControllerBase
{
    private readonly IStorageProvider _storage;
    private readonly LinkVaultDbContext _db;
    private readonly IAuditService _audit;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<ChunkedUploadController> _logger;

    // In-memory session tracking (use Redis in production for multi-instance)
    private static readonly ConcurrentDictionary<string, UploadSession> _sessions = new();

    public ChunkedUploadController(IStorageProvider storage, LinkVaultDbContext db,
        IAuditService audit, IFileValidationService fileValidation, ILogger<ChunkedUploadController> logger)
    {
        _storage = storage;
        _db = db;
        _audit = audit;
        _fileValidation = fileValidation;
        _logger = logger;
    }

    /// <summary>Initialize a chunked upload session.</summary>
    [HttpPost("init")]
    [Authorize(Policy = "CanUpload")]
    public ActionResult InitUpload([FromBody] InitUploadRequest request)
    {
        var userId = GetUserId();
        var uploadId = Guid.NewGuid().ToString("N");

        var session = new UploadSession
        {
            UploadId = uploadId,
            FileName = request.FileName,
            MimeType = request.MimeType,
            TotalSize = request.TotalSize,
            TotalChunks = request.TotalChunks,
            DirectoryId = request.DirectoryId,
            UserId = userId,
            ChunkSize = request.ChunkSize > 0 ? request.ChunkSize : 5 * 1024 * 1024, // default 5MB
            CompletedChunks = new ConcurrentDictionary<int, bool>(),
            CreatedAt = DateTime.UtcNow
        };

        _sessions[uploadId] = session;

        _logger.LogInformation("Upload session {UploadId} initialized for {FileName} ({TotalChunks} chunks)",
            uploadId, request.FileName, request.TotalChunks);

        return Ok(new
        {
            uploadId,
            chunkSize = session.ChunkSize,
            totalChunks = request.TotalChunks,
            message = "Upload session created. Start uploading chunks."
        });
    }

    /// <summary>Upload a single chunk.</summary>
    [HttpPut("{uploadId}/chunk/{chunkIndex}")]
    [Authorize(Policy = "CanUpload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB per chunk max
    public async Task<ActionResult> UploadChunk(string uploadId, int chunkIndex, IFormFile chunk)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return NotFound(new { error = "Upload session not found or expired." });

        if (session.UserId != GetUserId())
            return Forbid();

        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
            return BadRequest(new { error = $"Invalid chunk index. Expected 0-{session.TotalChunks - 1}." });

        if (chunk == null || chunk.Length == 0)
            return BadRequest(new { error = "Empty chunk." });

        // Save chunk to temp storage
        var chunkPath = $"_chunks/{uploadId}/chunk_{chunkIndex:D5}";
        await using var stream = chunk.OpenReadStream();
        await _storage.SaveAsync(stream, chunkPath);

        session.CompletedChunks[chunkIndex] = true;

        return Ok(new
        {
            chunkIndex,
            received = true,
            completedChunks = session.CompletedChunks.Count,
            totalChunks = session.TotalChunks,
            progress = Math.Round((double)session.CompletedChunks.Count / session.TotalChunks * 100, 1)
        });
    }

    /// <summary>Check upload progress (for resume).</summary>
    [HttpGet("{uploadId}/status")]
    public ActionResult GetStatus(string uploadId)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return NotFound(new { error = "Upload session not found." });

        return Ok(new
        {
            uploadId,
            fileName = session.FileName,
            totalChunks = session.TotalChunks,
            completedChunks = session.CompletedChunks.Keys.OrderBy(k => k).ToList(),
            missingChunks = Enumerable.Range(0, session.TotalChunks)
                .Where(i => !session.CompletedChunks.ContainsKey(i)).ToList(),
            progress = Math.Round((double)session.CompletedChunks.Count / session.TotalChunks * 100, 1)
        });
    }

    /// <summary>Finalize upload — merge chunks, create Resource record.</summary>
    [HttpPost("{uploadId}/complete")]
    [Authorize(Policy = "CanUpload")]
    public async Task<ActionResult> CompleteUpload(string uploadId)
    {
        if (!_sessions.TryGetValue(uploadId, out var session))
            return NotFound(new { error = "Upload session not found." });

        if (session.UserId != GetUserId())
            return Forbid();

        // Verify all chunks are present
        var missing = Enumerable.Range(0, session.TotalChunks)
            .Where(i => !session.CompletedChunks.ContainsKey(i)).ToList();

        if (missing.Count > 0)
            return BadRequest(new { error = "Upload incomplete.", missingChunks = missing });

        using var mergedStream = new MemoryStream();
        using var sha256 = SHA256.Create();
        
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = $"_chunks/{uploadId}/chunk_{i:D5}";
            await using var chunkStream = await _storage.GetAsync(chunkPath);
            await chunkStream.CopyToAsync(mergedStream);
        }

        mergedStream.Position = 0;

        // Validate merged file content (magic bytes, executable checks, SVG sanitization)
        var validation = _fileValidation.ValidateContent(mergedStream, session.MimeType, session.FileName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Chunked upload {UploadId} validation failed for {FileName}: {Error}",
                uploadId, session.FileName, validation.ErrorMessage);

            // Purge temporary chunks
            for (int i = 0; i < session.TotalChunks; i++)
            {
                var chunkPath = $"_chunks/{uploadId}/chunk_{i:D5}";
                await _storage.DeleteAsync(chunkPath);
            }

            _sessions.TryRemove(uploadId, out _);
            return BadRequest(new { error = validation.ErrorMessage ?? "Invalid file content." });
        }

        mergedStream.Position = 0;
        var checksum = Convert.ToHexString(await sha256.ComputeHashAsync(mergedStream)).ToLowerInvariant();

        var safeName = _fileValidation.SanitizeFileName(session.FileName);
        var ext = Path.GetExtension(session.FileName);
        var finalPath = $"{session.UserId}/{safeName}_{Guid.NewGuid():N}{ext}";

        mergedStream.Position = 0;
        await _storage.SaveAsync(mergedStream, finalPath);

        // Cleanup chunks
        for (int i = 0; i < session.TotalChunks; i++)
        {
            var chunkPath = $"_chunks/{uploadId}/chunk_{i:D5}";
            await _storage.DeleteAsync(chunkPath);
        }

        // Determine file type
        var fileType = DetermineFileType(session.MimeType);

        // Create resource record
        var resource = new Resource
        {
            Name = session.FileName,
            Type = ResourceType.File,
            FileType = fileType,
            StoragePath = $"/uploads/{finalPath}",
            MimeType = session.MimeType,
            SizeInBytes = session.TotalSize,
            ChecksumSHA256 = checksum,
            DirectoryId = session.DirectoryId,
            CreatedById = session.UserId,
            OrganizationId = GetOrgId(),
            IsActive = true
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        // Audit log
        await _audit.LogAsync(
            AuditActionType.Uploaded, ResourceEntityType.Resource,
            resource.Id, resource.Name,
            $"Chunked upload: {session.FileName} ({session.TotalChunks} chunks, {FormatSize(session.TotalSize)})",
            null, null, GetOrgId(), session.UserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        // Remove session
        _sessions.TryRemove(uploadId, out _);

        _logger.LogInformation("Chunked upload {UploadId} completed: {FileName} ({Size})",
            uploadId, session.FileName, FormatSize(session.TotalSize));

        return Ok(new
        {
            id = resource.Id,
            name = resource.Name,
            url = resource.StoragePath,
            mimeType = resource.MimeType,
            size = resource.SizeInBytes,
            checksum,
            createdAt = resource.CreatedAt
        });
    }

    private FileType DetermineFileType(string mimeType)
    {
        if (mimeType.StartsWith("image/")) return FileType.Image;
        if (mimeType.StartsWith("video/")) return FileType.Video;
        if (mimeType.StartsWith("audio/")) return FileType.Audio;
        if (mimeType.Contains("pdf") || mimeType.Contains("document") || mimeType.Contains("text"))
            return FileType.Document;
        if (mimeType.Contains("zip") || mimeType.Contains("archive") || mimeType.Contains("tar"))
            return FileType.Archive;
        return FileType.Other;
    }

    private string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:0.##} {units[i]}";
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid user claim.");
        return userId;
    }

    private Guid GetOrgId()
    {
        var claim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(claim, out var orgId) || orgId == Guid.Empty)
            throw new UnauthorizedAccessException("Invalid organization claim.");
        return orgId;
    }
}

// ── Request / Session models ──

public class InitUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
    public long TotalSize { get; set; }
    public int TotalChunks { get; set; }
    public int ChunkSize { get; set; }
    public Guid? DirectoryId { get; set; }
}

public class UploadSession
{
    public string UploadId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public int TotalChunks { get; set; }
    public int ChunkSize { get; set; }
    public Guid? DirectoryId { get; set; }
    public Guid UserId { get; set; }
    public ConcurrentDictionary<int, bool> CompletedChunks { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
