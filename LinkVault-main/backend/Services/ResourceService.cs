using System.Text.Json;
using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Services;

public class ResourceService : IResourceService
{
    private readonly LinkVaultDbContext _context;

    public ResourceService(LinkVaultDbContext context)
    {
        _context = context;
    }

    // Directories
    public async Task<List<DirectoryDto>> GetDirectoriesAsync(Guid organizationId, Guid? parentId = null)
    {
        var query = _context.Directories
            .Where(d => d.OrganizationId == organizationId && d.IsActive);

        if (parentId.HasValue && parentId.Value != Guid.Empty)
        {
            query = query.Where(d => d.ParentDirectoryId == parentId.Value);
        }
        else
        {
            query = query.Where(d => d.ParentDirectoryId == null);
        }

        var directories = await query
            .Include(d => d.Resources)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return directories.Select(MapToDirectoryDto).ToList();
    }

    public async Task<DirectoryDto?> GetDirectoryAsync(Guid id)
    {
        var directory = await _context.Directories
            .Include(d => d.Resources)
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        return directory == null ? null : MapToDirectoryDto(directory);
    }

    public async Task<DirectoryDto> CreateDirectoryAsync(Guid organizationId, Guid userId, CreateDirectoryRequestDto request)
    {
        var cleanParentId = (request.ParentDirectoryId == Guid.Empty) ? null : request.ParentDirectoryId;
        var directory = new Models.Directory
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            Color = request.Color,
            IsPublic = request.IsPublic,
            ExportRule = request.ExportRule,
            ParentDirectoryId = cleanParentId,
            OrganizationId = organizationId,
            CreatedById = userId
        };

        _context.Directories.Add(directory);
        await _context.SaveChangesAsync();

        return MapToDirectoryDto(directory);
    }

    public async Task<DirectoryDto?> UpdateDirectoryAsync(Guid id, Guid userId, UpdateDirectoryRequestDto request)
    {
        var directory = await _context.Directories.FindAsync(id);
        if (directory == null) return null;

        if (request.Name != null)
            directory.Name = request.Name;
        if (request.Description != null)
            directory.Description = request.Description;
        if (request.Icon != null)
            directory.Icon = request.Icon;
        if (request.Color != null)
            directory.Color = request.Color;
        if (request.IsPublic.HasValue)
            directory.IsPublic = request.IsPublic.Value;
        if (request.ExportRule.HasValue)
            directory.ExportRule = request.ExportRule.Value;

        directory.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDirectoryDto(directory);
    }

    public async Task<bool> DeleteDirectoryAsync(Guid id, Guid userId)
    {
        var directory = await _context.Directories
            .Include(d => d.Resources)
            .Include(d => d.SubDirectories)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (directory == null) return false;
        if (directory.Resources.Any() || directory.SubDirectories.Any()) return false;

        directory.IsActive = false;
        directory.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // Resources
    public async Task<List<ResourceDto>> GetResourcesAsync(Guid directoryId)
    {
        var resources = await _context.Resources
            .Where(r => r.DirectoryId == directoryId && r.IsActive && !r.IsDeleted)
            .Include(r => r.CreatedBy)
            .Include(r => r.Directory)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return resources.Select(MapToResourceDto).ToList();
    }

    public async Task<ResourceDto?> GetResourceAsync(Guid id)
    {
        var resource = await _context.Resources
            .Include(r => r.CreatedBy)
            .Include(r => r.Directory)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive && !r.IsDeleted);

        return resource == null ? null : MapToResourceDto(resource);
    }

    public async Task<ResourceDto> CreateResourceAsync(Guid organizationId, Guid userId, CreateResourceRequestDto request)
    {
        var resource = new Resource
        {
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            Url = request.Url,
            StoragePath = request.StoragePath,
            MimeType = request.MimeType,
            SizeInBytes = request.SizeInBytes,
            IsPublic = request.IsPublic,
            ExpiresAt = request.ExpiresAt,
            DirectoryId = request.DirectoryId,
            OrganizationId = organizationId,
            CreatedById = userId
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        return MapToResourceDto(resource);
    }

    public async Task<ResourceDto?> UpdateResourceAsync(Guid id, Guid userId, UpdateResourceRequestDto request)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null) return null;

        if (request.Name != null)
            resource.Name = request.Name;
        if (request.Description != null)
            resource.Description = request.Description;
        if (request.IsPublic.HasValue)
            resource.IsPublic = request.IsPublic.Value;
        if (request.ExpiresAt.HasValue)
            resource.ExpiresAt = request.ExpiresAt.Value;

        resource.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResourceDto(resource);
    }

    public async Task<bool> DeleteResourceAsync(Guid id, Guid userId)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.IsDeleted = true;
        resource.DeletedAt = DateTime.UtcNow;
        resource.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IncrementViewCountAsync(Guid id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.ViewCount++;
        resource.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IncrementClickCountAsync(Guid id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null) return false;

        resource.ClickCount++;
        resource.LastAccessedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // Links
    public async Task<List<LinkDto>> GetLinksAsync(Guid userId)
    {
        var links = await _context.Links
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.DisplayOrder)
            .ThenByDescending(l => l.CreatedAt)
            .ToListAsync();

        return links.Select(MapToLinkDto).ToList();
    }

    public async Task<LinkDto?> GetLinkAsync(Guid id)
    {
        var link = await _context.Links.FindAsync(id);
        return link == null ? null : MapToLinkDto(link);
    }

    public async Task<LinkDto> CreateLinkAsync(Guid userId, Guid organizationId, CreateLinkRequestDto request)
    {
        if (!Enum.TryParse<LinkType>(request.LinkType, true, out var linkType))
            linkType = LinkType.Standard;

        var link = new Link
        {
            Title = request.Title,
            Url = request.Url,
            Description = request.Description,
            Color = request.Color,
            Icon = request.Icon,
            LinkType = linkType,
            EmbedHtml = request.EmbedHtml,
            DisplayOrder = request.DisplayOrder,
            IsFeatured = request.IsFeatured,
            ScheduledAt = request.ScheduledAt,
            ExpiresAt = request.ExpiresAt,
            CodeLock = request.CodeLock,
            AnimationStyle = request.AnimationStyle,
            UserId = userId,
            OrganizationId = organizationId
        };

        _context.Links.Add(link);
        await _context.SaveChangesAsync();

        return MapToLinkDto(link);
    }

    public async Task<LinkDto?> UpdateLinkAsync(Guid id, Guid userId, UpdateLinkRequestDto request)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (link == null) return null;

        if (request.Title != null)
            link.Title = request.Title;
        if (request.Url != null)
            link.Url = request.Url;
        if (request.Description != null)
            link.Description = request.Description;
        if (request.Color != null)
            link.Color = request.Color;
        if (request.Icon != null)
            link.Icon = request.Icon;
        if (request.LinkType != null && Enum.TryParse<LinkType>(request.LinkType, true, out var lt))
            link.LinkType = lt;
        if (request.EmbedHtml != null)
            link.EmbedHtml = request.EmbedHtml;
        if (request.DisplayOrder.HasValue)
            link.DisplayOrder = request.DisplayOrder.Value;
        if (request.IsActive.HasValue)
            link.IsActive = request.IsActive.Value;
        if (request.IsFeatured.HasValue)
            link.IsFeatured = request.IsFeatured.Value;
        if (request.IsVisible.HasValue)
            link.IsVisible = request.IsVisible.Value;
        if (request.ScheduledAt.HasValue)
            link.ScheduledAt = request.ScheduledAt.Value;
        if (request.ExpiresAt.HasValue)
            link.ExpiresAt = request.ExpiresAt.Value;
        if (request.CodeLock != null)
            link.CodeLock = string.IsNullOrEmpty(request.CodeLock) ? null : request.CodeLock;
        if (request.AnimationStyle != null)
            link.AnimationStyle = request.AnimationStyle;

        link.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToLinkDto(link);
    }

    public async Task<bool> DeleteLinkAsync(Guid id, Guid userId)
    {
        var link = await _context.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (link == null) return false;

        _context.Links.Remove(link);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IncrementLinkClickAsync(Guid id)
    {
        var link = await _context.Links.FindAsync(id);
        if (link == null) return false;

        link.ClickCount++;
        link.LastClickedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReorderLinksAsync(Guid userId, List<Guid> linkIds)
    {
        var links = await _context.Links
            .Where(l => l.UserId == userId && linkIds.Contains(l.Id))
            .ToListAsync();

        for (int i = 0; i < linkIds.Count; i++)
        {
            var link = links.FirstOrDefault(l => l.Id == linkIds[i]);
            if (link != null)
            {
                link.DisplayOrder = i;
                link.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyLinkCodeAsync(Guid linkId, string code)
    {
        var link = await _context.Links.FindAsync(linkId);
        if (link == null) return false;
        if (string.IsNullOrEmpty(link.CodeLock)) return true; // no lock
        return link.CodeLock == code;
    }

    // Public Profile
    public async Task<PublicProfileDto?> GetPublicProfileAsync(string usernameOrSlug)
    {
        var user = await _context.Users
            .Include(u => u.Links)
            .Include(u => u.ProfileTheme)
            .FirstOrDefaultAsync(u => 
                (u.Username == usernameOrSlug || u.PublicPageSlug == usernameOrSlug) 
                && u.IsActive);

        if (user == null) return null;

        var now = DateTime.UtcNow;
        var visibleLinks = user.Links
            .Where(l => l.IsActive && l.IsVisible)
            .Where(l => !l.ScheduledAt.HasValue || l.ScheduledAt.Value <= now)
            .Where(l => !l.ExpiresAt.HasValue || l.ExpiresAt.Value > now)
            .OrderBy(l => l.DisplayOrder)
            .Select(MapToPublicLinkDto)
            .ToList();

        // Parse social links JSON
        Dictionary<string, string>? socialLinks = null;
        if (!string.IsNullOrEmpty(user.SocialLinks))
        {
            try { socialLinks = JsonSerializer.Deserialize<Dictionary<string, string>>(user.SocialLinks); }
            catch { /* ignore malformed JSON */ }
        }

        return new PublicProfileDto
        {
            Username = user.Username,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            PublicPageSlug = user.PublicPageSlug,
            SocialLinks = socialLinks,
            Theme = user.ProfileTheme != null ? MapToProfileThemeDto(user.ProfileTheme) : null,
            Links = visibleLinks
        };
    }

    // Profile Theme Management
    public async Task<ProfileThemeDto?> GetProfileThemeAsync(Guid userId)
    {
        var theme = await _context.ProfileThemes
            .FirstOrDefaultAsync(pt => pt.UserId == userId);
        return theme == null ? null : MapToProfileThemeDto(theme);
    }

    public async Task<ProfileThemeDto> UpsertProfileThemeAsync(Guid userId, UpdateProfileThemeDto request)
    {
        var theme = await _context.ProfileThemes
            .FirstOrDefaultAsync(pt => pt.UserId == userId);

        if (theme == null)
        {
            theme = new ProfileTheme { UserId = userId };
            _context.ProfileThemes.Add(theme);
        }

        if (request.BackgroundType != null && Enum.TryParse<BackgroundType>(request.BackgroundType, true, out var bg))
            theme.BackgroundType = bg;
        if (request.BackgroundValue != null)
            theme.BackgroundValue = request.BackgroundValue;
        if (request.ButtonStyle != null && Enum.TryParse<ButtonStyle>(request.ButtonStyle, true, out var bs))
            theme.ButtonStyle = bs;
        if (request.ButtonColor != null)
            theme.ButtonColor = request.ButtonColor;
        if (request.ButtonTextColor != null)
            theme.ButtonTextColor = request.ButtonTextColor;
        if (request.ButtonHoverColor != null)
            theme.ButtonHoverColor = request.ButtonHoverColor;
        if (request.FontFamily != null)
            theme.FontFamily = request.FontFamily;
        if (request.ProfileLayout != null && Enum.TryParse<ProfileLayout>(request.ProfileLayout, true, out var pl))
            theme.ProfileLayout = pl;
        if (request.SocialLinksPosition != null && Enum.TryParse<SocialLinksPosition>(request.SocialLinksPosition, true, out var sp))
            theme.SocialLinksPosition = sp;
        if (request.CustomCSS != null)
            theme.CustomCSS = request.CustomCSS;
        if (request.HideBranding.HasValue)
            theme.HideBranding = request.HideBranding.Value;

        theme.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToProfileThemeDto(theme);
    }

    public async Task<bool> UpdateSocialLinksAsync(Guid userId, Dictionary<string, string> socialLinks)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.SocialLinks = JsonSerializer.Serialize(socialLinks);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePublicSlugAsync(Guid userId, string slug)
    {
        // Check uniqueness
        var existing = await _context.Users
            .AnyAsync(u => u.PublicPageSlug == slug && u.Id != userId);
        if (existing) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PublicPageSlug = slug;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // Helper methods
    private DirectoryDto MapToDirectoryDto(Models.Directory directory)
    {
        return new DirectoryDto
        {
            Id = directory.Id,
            Name = directory.Name,
            Description = directory.Description,
            Icon = directory.Icon,
            Color = directory.Color,
            IsPublic = directory.IsPublic,
            ExportRule = directory.ExportRule.ToString(),
            ParentDirectoryId = directory.ParentDirectoryId,
            OrganizationId = directory.OrganizationId,
            ResourceCount = directory.Resources?.Count(r => r.IsActive && !r.IsDeleted) ?? 0,
            CreatedAt = directory.CreatedAt
        };
    }

    private ResourceDto MapToResourceDto(Resource resource)
    {
        return new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Type = resource.Type.ToString(),
            FileType = resource.FileType?.ToString(),
            Url = resource.Url,
            StoragePath = resource.StoragePath,
            MimeType = resource.MimeType,
            SizeInBytes = resource.SizeInBytes,
            SizeFormatted = FormatFileSize(resource.SizeInBytes),
            ThumbnailUrl = resource.ThumbnailUrl,
            Description = resource.Description,
            ViewCount = resource.ViewCount,
            ClickCount = resource.ClickCount,
            IsPublic = resource.IsPublic,
            ExpiresAt = resource.ExpiresAt,
            DirectoryId = resource.DirectoryId,
            DirectoryName = resource.Directory?.Name ?? string.Empty,
            CreatedById = resource.CreatedById,
            CreatedByName = resource.CreatedBy?.DisplayName ?? resource.CreatedBy?.Username ?? string.Empty,
            CreatedAt = resource.CreatedAt,
            OrganizationId = resource.OrganizationId
        };
    }


    private LinkDto MapToLinkDto(Link link)
    {
        return new LinkDto
        {
            Id = link.Id,
            Title = link.Title,
            Url = link.Url,
            Description = link.Description,
            ThumbnailUrl = link.ThumbnailUrl,
            Color = link.Color,
            Icon = link.Icon,
            LinkType = link.LinkType.ToString(),
            EmbedHtml = link.EmbedHtml,
            DisplayOrder = link.DisplayOrder,
            ClickCount = link.ClickCount,
            IsActive = link.IsActive,
            IsFeatured = link.IsFeatured,
            IsVisible = link.IsVisible,
            ScheduledAt = link.ScheduledAt,
            ExpiresAt = link.ExpiresAt,
            HasCodeLock = !string.IsNullOrEmpty(link.CodeLock),
            AnimationStyle = link.AnimationStyle,
            CreatedAt = link.CreatedAt
        };
    }

    private PublicLinkDto MapToPublicLinkDto(Link link)
    {
        return new PublicLinkDto
        {
            Id = link.Id,
            Title = link.Title,
            Url = link.Url,
            Description = link.Description,
            ThumbnailUrl = link.ThumbnailUrl,
            Color = link.Color,
            Icon = link.Icon,
            LinkType = link.LinkType.ToString(),
            EmbedHtml = link.EmbedHtml,
            DisplayOrder = link.DisplayOrder,
            ClickCount = link.ClickCount,
            IsFeatured = link.IsFeatured,
            HasCodeLock = !string.IsNullOrEmpty(link.CodeLock),
            AnimationStyle = link.AnimationStyle
        };
    }

    private ProfileThemeDto MapToProfileThemeDto(ProfileTheme theme)
    {
        return new ProfileThemeDto
        {
            Id = theme.Id,
            BackgroundType = theme.BackgroundType.ToString(),
            BackgroundValue = theme.BackgroundValue,
            ButtonStyle = theme.ButtonStyle.ToString(),
            ButtonColor = theme.ButtonColor,
            ButtonTextColor = theme.ButtonTextColor,
            ButtonHoverColor = theme.ButtonHoverColor,
            FontFamily = theme.FontFamily,
            ProfileLayout = theme.ProfileLayout.ToString(),
            SocialLinksPosition = theme.SocialLinksPosition.ToString(),
            CustomCSS = theme.CustomCSS,
            HideBranding = theme.HideBranding
        };
    }

    private string FormatFileSize(long? bytes)
    {
        if (!bytes.HasValue) return "-";
        
        var size = bytes.Value;
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}
