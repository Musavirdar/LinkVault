using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShareController : ControllerBase
{
    private readonly LinkVaultDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly IAuditService _audit;
    private readonly ILogger<ShareController> _logger;

    public ShareController(LinkVaultDbContext db, IStorageProvider storage,
        IAuditService audit, ILogger<ShareController> logger)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>Create a share link for a resource.</summary>
    [HttpPost]
    [Authorize(Policy = "CanShare")]
    public async Task<ActionResult> CreateShareLink([FromBody] CreateShareRequest request)
    {
        var userId = GetUserId();
        var orgId = GetOrganizationId();
        var resource = await _db.Resources
            .FirstOrDefaultAsync(r => r.Id == request.ResourceId && r.OrganizationId == orgId && r.IsActive && !r.IsDeleted);

        if (resource == null) return NotFound(new { error = "Resource not found." });

        string? passwordHash = null;
        if (!string.IsNullOrEmpty(request.Password))
        {
            passwordHash = HashPassword(request.Password);
        }

        var share = new ShareLink
        {
            ResourceId = request.ResourceId,
            OrganizationId = orgId,
            PasswordHash = passwordHash,
            ExpiresAt = request.ExpiresAt,
            MaxDownloads = request.MaxDownloads,
            AllowPreview = request.AllowPreview,
            RequireEmail = request.RequireEmail,
            CreatedById = userId
        };

        _db.ShareLinks.Add(share);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            AuditActionType.Shared, ResourceEntityType.Resource,
            resource.Id, resource.Name,
            $"Created share link: {share.Token}",
            null, null, resource.OrganizationId, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(new
        {
            shareId = share.Id,
            token = share.Token,
            url = $"/s/{share.Token}",
            expiresAt = share.ExpiresAt,
            maxDownloads = share.MaxDownloads,
            hasPassword = !string.IsNullOrEmpty(passwordHash),
            allowPreview = share.AllowPreview,
            requireEmail = share.RequireEmail
        });
    }

    /// <summary>Get share link metadata (public).</summary>
    [HttpGet("{token}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetShareInfo(string token)
    {
        var share = await _db.ShareLinks
            .Include(s => s.Resource)
            .Include(s => s.CreatedBy)
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

        if (share == null) return NotFound(new { error = "Share link not found." });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return Gone(new { error = "This share link has expired." });

        if (share.MaxDownloads.HasValue && share.CurrentDownloads >= share.MaxDownloads.Value)
            return Gone(new { error = "Download limit reached." });

        return Ok(new
        {
            token = share.Token,
            fileName = share.Resource.Name,
            fileType = share.Resource.FileType?.ToString(),
            mimeType = share.Resource.MimeType,
            sizeInBytes = share.Resource.SizeInBytes,
            sizeFormatted = FormatSize(share.Resource.SizeInBytes ?? 0),
            hasPassword = !string.IsNullOrEmpty(share.PasswordHash),
            allowPreview = share.AllowPreview,
            requireEmail = share.RequireEmail,
            expiresAt = share.ExpiresAt,
            downloadsRemaining = share.MaxDownloads.HasValue
                ? share.MaxDownloads.Value - share.CurrentDownloads
                : (int?)null,
            sharedBy = share.CreatedBy?.DisplayName ?? share.CreatedBy?.Username
        });
    }

    /// <summary>Verify password for a protected share link.</summary>
    [HttpPost("{token}/verify")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyPassword(string token, [FromBody] VerifySharePasswordRequest request)
    {
        var share = await _db.ShareLinks.FirstOrDefaultAsync(s => s.Token == token && s.IsActive);
        if (share == null) return NotFound(new { error = "Share link not found." });

        if (string.IsNullOrEmpty(share.PasswordHash))
            return Ok(new { valid = true, message = "No password required." });

        var valid = VerifyHash(request.Password, share.PasswordHash);
        if (!valid) return BadRequest(new { valid = false, error = "Incorrect password." });

        return Ok(new { valid = true });
    }

    /// <summary>Download a shared file (increments download count).</summary>
    [HttpPost("{token}/download")]
    [AllowAnonymous]
    public async Task<ActionResult> Download(string token, [FromBody] DownloadShareRequest? request = null)
    {
        var share = await _db.ShareLinks
            .Include(s => s.Resource)
            .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

        if (share == null) return NotFound(new { error = "Share link not found." });

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return Gone(new { error = "This share link has expired." });

        if (share.MaxDownloads.HasValue && share.CurrentDownloads >= share.MaxDownloads.Value)
            return Gone(new { error = "Download limit reached." });

        // Verify password if required
        if (!string.IsNullOrEmpty(share.PasswordHash))
        {
            if (request == null || string.IsNullOrEmpty(request.Password))
                return Unauthorized(new { error = "Password required." });
            if (!VerifyHash(request.Password, share.PasswordHash))
                return BadRequest(new { error = "Incorrect password." });
        }

        // Verify email if required
        if (share.RequireEmail && (request == null || string.IsNullOrEmpty(request.Email)))
            return BadRequest(new { error = "Email required to download." });

        // Create single-use download ticket
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var ticketToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var ticket = new DownloadTicket
        {
            Token = ticketToken,
            ResourceId = share.ResourceId,
            UserId = share.CreatedById,
            OrganizationId = share.OrganizationId,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(60),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.DownloadTickets.Add(ticket);

        // Increment download count
        share.CurrentDownloads++;
        share.Resource.DownloadCount++;
        await _db.SaveChangesAsync();

        // Return authenticated ticket download URL
        return Ok(new
        {
            downloadUrl = $"/api/files/{share.ResourceId}/download?ticket={ticketToken}",
            fileName = share.Resource.Name,
            mimeType = share.Resource.MimeType,
            downloadsRemaining = share.MaxDownloads.HasValue
                ? share.MaxDownloads.Value - share.CurrentDownloads
                : (int?)null
        });
    }

    /// <summary>List all share links created by the current user.</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult> GetMyShareLinks()
    {
        var userId = GetUserId();
        var shares = await _db.ShareLinks
            .Include(s => s.Resource)
            .Where(s => s.CreatedById == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                id = s.Id,
                token = s.Token,
                fileName = s.Resource.Name,
                expiresAt = s.ExpiresAt,
                maxDownloads = s.MaxDownloads,
                currentDownloads = s.CurrentDownloads,
                hasPassword = s.PasswordHash != null,
                createdAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(shares);
    }

    /// <summary>Revoke (deactivate) a share link.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> RevokeShareLink(Guid id)
    {
        var userId = GetUserId();
        var share = await _db.ShareLinks.FirstOrDefaultAsync(s => s.Id == id && s.CreatedById == userId);
        if (share == null) return NotFound();

        share.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ──

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool VerifyHash(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private ActionResult Gone(object value) => StatusCode(410, value);

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        int i = 0; double s = bytes;
        while (s >= 1024 && i < units.Length - 1) { s /= 1024; i++; }
        return $"{s:0.##} {units[i]}";
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid user claim.");
        return userId;
    }

    private Guid GetOrganizationId()
    {
        var claim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(claim, out var orgId))
            throw new UnauthorizedAccessException("Invalid organization claim.");
        return orgId;
    }
}

// ── Request DTOs ──

public class CreateShareRequest
{
    public Guid ResourceId { get; set; }
    public string? Password { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxDownloads { get; set; }
    public bool AllowPreview { get; set; } = true;
    public bool RequireEmail { get; set; } = false;
}

public class VerifySharePasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public class DownloadShareRequest
{
    public string? Password { get; set; }
    public string? Email { get; set; }
}
