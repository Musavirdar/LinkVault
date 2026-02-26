using LinkVault.Api.Extensions;
using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Links;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LinksController : ControllerBase
{
    private readonly ILinkService _linkService;

    public LinksController(ILinkService linkService) => _linkService = linkService;

    [HttpPost]
    public async Task<ActionResult<LinkDto>> Create([FromBody] CreateLinkRequest request)
    {
        var link = await _linkService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = link.Id }, link);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LinkDto>> GetById(Guid id)
    {
        var link = await _linkService.GetByIdAsync(User.GetUserId(), id);
        return Ok(link);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LinkDto>>> GetAll([FromQuery] Guid? directoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var links = await _linkService.GetUserLinksAsync(User.GetUserId(), directoryId, page, pageSize);
        return Ok(links);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LinkDto>> Update(Guid id, [FromBody] UpdateLinkRequest request)
    {
        var link = await _linkService.UpdateAsync(User.GetUserId(), id, request);
        return Ok(link);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _linkService.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("info/{shortCode}")]
    public async Task<ActionResult<LinkDto>> GetByShortCode(string shortCode)
    {
        var link = await _linkService.GetByShortCodeAsync(shortCode);
        return link == null ? NotFound() : Ok(link);
    }
}
