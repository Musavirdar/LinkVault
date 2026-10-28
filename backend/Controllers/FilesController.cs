using System.Security.Claims;
using System.Security.Cryptography;
using LinkVaultApi.Data;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly LinkVaultDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly IAuditService _audit;
    private readonly ILogger<FilesController> _logger;
    private static readonly object _inMemoryTicketLock = new();

    public FilesController(
        LinkVaultDbContext db,
        IStorageProvider storage,
        IAuditService audit,
        ILogger<FilesController> logger)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Issue a short-lived single-use download ticket for browser-native downloads.
    /// </summary>
    [HttpPost("{id:guid}/ticket")]
    [HttpPost("{id:guid}/download-ticket")]
    [Authorize]
    public async Task<ActionResult> CreateDownloadTicket(Guid id)
    {
        var userId = GetCurrentUserId();
        var orgId = GetCurrentOrganizationId();

        var resource = await _db.Resources
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive && !r.IsDeleted);

        if (resource == null)
        {
            return NotFound(new { error = "Resource not found." });
        }

        if (resource.OrganizationId != orgId)
        {
            return Forbid();
        }

        // Generate 32 cryptographically secure bytes -> base64url string
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var ticket = new DownloadTicket
        {
            Token = token,
            ResourceId = resource.Id,
            UserId = userId,
            OrganizationId = orgId,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(60),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.DownloadTickets.Add(ticket);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            AuditActionType.Downloaded,
            ResourceEntityType.Resource,
            resource.Id,
            resource.Name,
            $"Download ticket issued: {ticket.Id}",
            null,
            null,
            orgId,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(new
        {
            ticket = ticket.Token,
            expiresAt = ticket.ExpiresAtUtc,
            downloadUrl = $"/api/files/{resource.Id}/download?ticket={ticket.Token}"
        });
    }

    /// <summary>
    /// Stream the requested file. Accepts Bearer JWT OR single-use ?ticket= query parameter.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadFile(Guid id, [FromQuery] string? ticket = null)
    {
        Resource? resource;
        Guid? effectiveUserId = null;
        Guid? effectiveOrgId = null;

        if (!string.IsNullOrWhiteSpace(ticket))
        {
            var now = DateTime.UtcNow;
            int updatedRows;

            if (_db.Database.IsRelational())
            {
                // T016: Atomic single-statement update in relational database (PostgreSQL)
                updatedRows = await _db.DownloadTickets
                    .Where(dt => dt.Token == ticket && dt.ResourceId == id && !dt.IsUsed && dt.ExpiresAtUtc > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsUsed, true));
            }
            else
            {
                // In-memory provider fallback (used in unit/integration testing where relational SQL translation is unsupported)
                lock (_inMemoryTicketLock)
                {
                    var ticketEntity = _db.DownloadTickets
                        .FirstOrDefault(dt => dt.Token == ticket && dt.ResourceId == id && !dt.IsUsed && dt.ExpiresAtUtc > now);

                    if (ticketEntity != null)
                    {
                        ticketEntity.IsUsed = true;
                        _db.SaveChanges();
                        updatedRows = 1;
                    }
                    else
                    {
                        updatedRows = 0;
                    }
                }
            }

            if (updatedRows == 0)
            {
                var existingTicket = await _db.DownloadTickets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(dt => dt.Token == ticket && dt.ResourceId == id);

                if (existingTicket == null)
                {
                    return Unauthorized(new { error = "Invalid download ticket." });
                }

                if (existingTicket.IsUsed)
                {
                    return Unauthorized(new { error = "Download ticket has already been used." });
                }

                if (existingTicket.ExpiresAtUtc <= now)
                {
                    return StatusCode(StatusCodes.Status410Gone, new { error = "Download ticket has expired." });
                }

                return Unauthorized(new { error = "Invalid download ticket." });
            }

            // Ticket claimed atomically. Fetch ticket record for organization and user info
            var claimedTicket = await _db.DownloadTickets
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Token == ticket && dt.ResourceId == id);

            if (claimedTicket == null)
            {
                return NotFound(new { error = "Download ticket not found." });
            }

            // T017: Recheck that target Resource is IsActive && !IsDeleted
            resource = await _db.Resources
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource == null || !resource.IsActive || resource.IsDeleted)
            {
                return NotFound(new { error = "Resource is no longer active or has been deleted." });
            }

            // Ensure ticket organization matches resource organization
            if (resource.OrganizationId != claimedTicket.OrganizationId)
            {
                return Forbid();
            }

            // T017: Recheck that ticket creator user is IsActive
            var creatorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == claimedTicket.UserId);
            if (creatorUser != null && !creatorUser.IsActive)
            {
                return Forbid();
            }

            effectiveUserId = claimedTicket.UserId;
            effectiveOrgId = claimedTicket.OrganizationId;
        }
        else
        {
            // Bearer JWT authentication path
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { error = "Authentication required." });
            }

            try
            {
                effectiveUserId = GetCurrentUserId();
                effectiveOrgId = GetCurrentOrganizationId();
            }
            catch
            {
                return Unauthorized(new { error = "Invalid user credentials." });
            }

            resource = await _db.Resources
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive && !r.IsDeleted);

            if (resource == null)
            {
                return NotFound(new { error = "Resource not found." });
            }

            if (resource.OrganizationId != effectiveOrgId)
            {
                return Forbid();
            }
        }

        if (resource == null || string.IsNullOrEmpty(resource.StoragePath))
        {
            return NotFound(new { error = "File not found." });
        }

        // Normalize path for storage provider
        var storagePath = resource.StoragePath.TrimStart('/');
        if (storagePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            storagePath = storagePath.Substring("uploads/".Length);
        }

        Stream fileStream;
        try
        {
            fileStream = await _storage.GetAsync(storagePath);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("File not found on storage provider: {StoragePath}", storagePath);
            return NotFound(new { error = "File content not found on server." });
        }

        // Increment download counter
        resource.DownloadCount++;
        resource.LastAccessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Enforce secure download headers
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        var mimeType = string.IsNullOrWhiteSpace(resource.MimeType) ? "application/octet-stream" : resource.MimeType;
        var safeFileName = Path.GetFileName(resource.Name);

        // Security requirement: SVG files must always be forced as attachment to prevent XSS
        var forceAttachment = mimeType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase) ||
                              mimeType.Equals("application/x-msdownload", StringComparison.OrdinalIgnoreCase);

        var dispositionType = forceAttachment ? "attachment" : "inline";
        Response.Headers["Content-Disposition"] = $"{dispositionType}; filename=\"{safeFileName}\"";

        if (resource.SizeInBytes.HasValue && resource.SizeInBytes.Value > 0)
        {
            Response.ContentLength = resource.SizeInBytes.Value;
        }

        return new FileStreamResult(fileStream, mimeType)
        {
            EnableRangeProcessing = true
        };
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid user claim.");
        return userId;
    }

    private Guid GetCurrentOrganizationId()
    {
        var claim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(claim, out var orgId))
            throw new UnauthorizedAccessException("Invalid organization claim.");
        return orgId;
    }
}
