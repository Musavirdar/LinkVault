using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using LinkVaultApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IFileValidationService _fileValidation;
    private readonly LinkVaultDbContext _db;
    private readonly IAuditService _audit;

    public FileUploadController(
        IFileService fileService,
        IFileValidationService fileValidation,
        LinkVaultDbContext db,
        IAuditService audit)
    {
        _fileService = fileService;
        _fileValidation = fileValidation;
        _db = db;
        _audit = audit;
    }

    /// <summary>
    /// Upload a file. Max size 10MB. Accepted MIME types are enforced server-side.
    /// Only users with the CanUpload permission (admins by default in Teams) may call this.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanUpload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid? directoryId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (file.Length > _fileService.MaxFileSizeBytes)
            return BadRequest(new { error = $"File exceeds maximum size of {_fileService.MaxFileSizeBytes / 1024 / 1024}MB." });

        if (!_fileService.AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new { error = "File type not permitted.", allowedTypes = _fileService.AllowedMimeTypes });

        // Validate magic bytes and content safety
        using (var stream = file.OpenReadStream())
        {
            var validation = _fileValidation.ValidateContent(stream, file.ContentType, file.FileName);
            if (!validation.IsValid)
            {
                return BadRequest(new { error = validation.ErrorMessage ?? "File content does not match declared MIME type." });
            }
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // Validate directory ownership if provided and non-empty
        var cleanDirectoryId = (directoryId == Guid.Empty) ? null : directoryId;
        if (cleanDirectoryId.HasValue)
        {
            var dir = await _db.Directories
                .FirstOrDefaultAsync(d => d.Id == cleanDirectoryId.Value && d.OrganizationId == GetOrgId());
            if (dir == null)
                return BadRequest(new { error = "Directory not found or access denied." });
        }

        var relativePath = await _fileService.SaveFileAsync(file, userId);
        var sanitizedFileName = _fileValidation.SanitizeFileName(file.FileName);

        // Create resource record
        var resource = new Models.Resource
        {
            Name = sanitizedFileName,
            Type = Models.ResourceType.File,
            StoragePath = relativePath,
            MimeType = file.ContentType,
            SizeInBytes = file.Length,
            DirectoryId = cleanDirectoryId,
            CreatedById = userId,
            OrganizationId = GetOrgId(),
            IsActive = true
        };

        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            AuditActionType.Uploaded,
            ResourceEntityType.Resource,
            resource.Id,
            resource.Name,
            $"Uploaded {sanitizedFileName}",
            null,
            null,
            GetOrgId(),
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()
        );

        return CreatedAtAction(nameof(Upload), new
        {
            id = resource.Id,
            name = resource.Name,
            url = $"/api/files/{resource.Id}/download",
            mimeType = resource.MimeType,
            size = resource.SizeInBytes,
            createdAt = resource.CreatedAt
        });
    }

    private Guid GetOrgId()
    {
        var claim = User.FindFirst("orgId")?.Value;
        if (!Guid.TryParse(claim, out var orgId) || orgId == Guid.Empty)
            throw new UnauthorizedAccessException("Invalid organization claim.");
        return orgId;
    }

}
