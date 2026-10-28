using LinkVaultApi.Dtos;

namespace LinkVaultApi.Services;

public interface IResourceService
{
    // Directories
    Task<List<DirectoryDto>> GetDirectoriesAsync(Guid organizationId, Guid? parentId = null);
    Task<DirectoryDto?> GetDirectoryAsync(Guid id);
    Task<DirectoryDto> CreateDirectoryAsync(Guid organizationId, Guid userId, CreateDirectoryRequestDto request);
    Task<DirectoryDto?> UpdateDirectoryAsync(Guid id, Guid userId, UpdateDirectoryRequestDto request);
    Task<bool> DeleteDirectoryAsync(Guid id, Guid userId);

    // Resources
    Task<List<ResourceDto>> GetResourcesAsync(Guid directoryId);
    Task<ResourceDto?> GetResourceAsync(Guid id);
    Task<ResourceDto> CreateResourceAsync(Guid organizationId, Guid userId, CreateResourceRequestDto request);
    Task<ResourceDto?> UpdateResourceAsync(Guid id, Guid userId, UpdateResourceRequestDto request);
    Task<bool> DeleteResourceAsync(Guid id, Guid userId);
    Task<bool> IncrementViewCountAsync(Guid id);
    Task<bool> IncrementClickCountAsync(Guid id);

    // Links
    Task<List<LinkDto>> GetLinksAsync(Guid userId);
    Task<LinkDto?> GetLinkAsync(Guid id);
    Task<LinkDto> CreateLinkAsync(Guid userId, Guid organizationId, CreateLinkRequestDto request);
    Task<LinkDto?> UpdateLinkAsync(Guid id, Guid userId, UpdateLinkRequestDto request);
    Task<bool> DeleteLinkAsync(Guid id, Guid userId);
    Task<bool> IncrementLinkClickAsync(Guid id);
    Task<bool> ReorderLinksAsync(Guid userId, List<Guid> linkIds);
    Task<bool> VerifyLinkCodeAsync(Guid linkId, string code);

    // Public Profile
    Task<PublicProfileDto?> GetPublicProfileAsync(string usernameOrSlug);

    // Profile Theme
    Task<ProfileThemeDto?> GetProfileThemeAsync(Guid userId);
    Task<ProfileThemeDto> UpsertProfileThemeAsync(Guid userId, UpdateProfileThemeDto request);
    Task<bool> UpdateSocialLinksAsync(Guid userId, Dictionary<string, string> socialLinks);
    Task<bool> UpdatePublicSlugAsync(Guid userId, string slug);
}
