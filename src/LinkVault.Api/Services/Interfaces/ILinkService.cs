using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Links;

namespace LinkVault.Api.Services.Interfaces;

public interface ILinkService
{
    Task<LinkDto> CreateAsync(Guid userId, CreateLinkRequest request);
    Task<LinkDto> GetByIdAsync(Guid userId, Guid id);
    Task<LinkDto?> GetByShortCodeAsync(string shortCode);
    Task<PagedResult<LinkDto>> GetUserLinksAsync(Guid userId, Guid? directoryId, int page, int pageSize);
    Task<LinkDto> UpdateAsync(Guid userId, Guid id, UpdateLinkRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task<string> ResolveAndTrackAsync(string shortCode);
}
