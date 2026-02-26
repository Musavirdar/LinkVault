using LinkVault.Api.Extensions;
using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Directories;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DirectoriesController : ControllerBase
{
    private readonly IDirectoryService _directoryService;

    public DirectoriesController(IDirectoryService directoryService) => _directoryService = directoryService;

    [HttpPost]
    public async Task<ActionResult<DirectoryDto>> Create([FromBody] CreateDirectoryRequest request)
    {
        var directory = await _directoryService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = directory.Id }, directory);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DirectoryDto>> GetById(Guid id)
    {
        var directory = await _directoryService.GetByIdAsync(User.GetUserId(), id);
        return Ok(directory);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DirectoryDto>>> GetAll([FromQuery] Guid? parentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var directories = await _directoryService.GetUserDirectoriesAsync(User.GetUserId(), parentId, page, pageSize);
        return Ok(directories);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DirectoryDto>> Update(Guid id, [FromBody] UpdateDirectoryRequest request)
    {
        var directory = await _directoryService.UpdateAsync(User.GetUserId(), id, request);
        return Ok(directory);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _directoryService.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
