using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly ILinkService _linkService;
    
    public RedirectController(ILinkService linkService)
    {
        _linkService = linkService;
    }
    
    [HttpGet("/{shortCode}")]
    public async Task<IActionResult> RedirectToOriginal(string shortCode)
    {
        var originalUrl = await _linkService.ResolveAndTrackAsync(shortCode);
        return RedirectPermanent(originalUrl);
    }
}
