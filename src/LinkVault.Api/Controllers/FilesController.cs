using LinkVault.Api.Extensions;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost]
    [RequestSizeLimit(104_857_600)] // 100MB
    public async Task<IActionResult> Upload([FromQuery] Guid directoryId, IFormFile file)
    {
        var result = await _fileService.UploadAsync(file, User.GetUserId(), directoryId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _fileService.GetByIdAsync(User.GetUserId(), id);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByDirectory(
        [FromQuery] Guid directoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _fileService.GetDirectoryFilesAsync(User.GetUserId(), directoryId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var (stream, contentType, fileName) = await _fileService.DownloadAsync(User.GetUserId(), id);
        return File(stream, contentType, fileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _fileService.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPost("{id}/share")]
    public async Task<IActionResult> CreateShareLink(Guid id, [FromBody] CreateShareLinkRequest request)
    {
        var token = await _fileService.CreateShareLinkAsync(
            User.GetUserId(), id, request.ExpiresAt, request.Password, request.MaxDownloads);
        return Ok(new { token });
    }
}

[ApiController]
[Route("api/shared")]
public class SharedFilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public SharedFilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> DownloadShared(string token, [FromQuery] string? password)
    {
        var (stream, contentType, fileName) = await _fileService.DownloadViaShareLinkAsync(token, password);
        return File(stream, contentType, fileName);
    }
}

public class CreateShareLinkRequest
{
    public DateTime? ExpiresAt { get; set; }
    public string? Password { get; set; }
    public int? MaxDownloads { get; set; }
}
