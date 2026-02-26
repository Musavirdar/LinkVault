using LinkVault.Api.Extensions;
using LinkVault.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public OrganizationsController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
    {
        var result = await _orgService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _orgService.GetByIdAsync(User.GetUserId(), id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        var result = await _orgService.UpdateAsync(User.GetUserId(), id, request);
        return Ok(result);
    }

    [HttpGet("{id}/members")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var result = await _orgService.GetMembersAsync(User.GetUserId(), id);
        return Ok(result);
    }

    [HttpPost("{id}/invite")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
    {
        await _orgService.InviteMemberAsync(User.GetUserId(), id, request);
        return Ok(new { message = "Invitation sent" });
    }

    [HttpDelete("{id}/members/{memberId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        await _orgService.RemoveMemberAsync(User.GetUserId(), id, memberId);
        return NoContent();
    }
}

[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public InvitationsController(IOrganizationService orgService)
    {
        _orgService = orgService;
    }

    [HttpPost("{token}/accept")]
    public async Task<IActionResult> Accept(string token, [FromBody] AcceptInvitationRequest request)
    {
        await _orgService.AcceptInvitationAsync(token, request);
        return Ok(new { message = "Account created successfully. Please log in." });
    }
}
