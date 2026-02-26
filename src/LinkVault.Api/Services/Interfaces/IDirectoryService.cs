using LinkVault.Api.Models.DTOs.Common;
using LinkVault.Api.Models.DTOs.Directories;

namespace LinkVault.Api.Services.Interfaces;

public interface IDirectoryService
{
    Task<DirectoryDto> CreateAsync(Guid userId, CreateDirectoryRequest request);
    Task<DirectoryDto> GetByIdAsync(Guid userId, Guid directoryId);
    Task<PagedResult<DirectoryDto>> GetUserDirectoriesAsync(Guid userId, Guid? parentId, int page, int pageSize);
    Task<DirectoryDto> UpdateAsync(Guid userId, Guid directoryId, UpdateDirectoryRequest request);
    Task DeleteAsync(Guid userId, Guid directoryId);
}
