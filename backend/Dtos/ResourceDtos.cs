using System.ComponentModel.DataAnnotations;
using LinkVaultApi.Models;

namespace LinkVaultApi.Dtos;

// Directory DTOs
public class DirectoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsPublic { get; set; }
    public string ExportRule { get; set; } = string.Empty;
    public Guid? ParentDirectoryId { get; set; }
    public Guid OrganizationId { get; set; }
    public int ResourceCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDirectoryRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public bool IsPublic { get; set; } = false;

    public ExportRule ExportRule { get; set; } = ExportRule.Allow;

    public Guid? ParentDirectoryId { get; set; }
}

public class UpdateDirectoryRequestDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    public bool? IsPublic { get; set; }

    public ExportRule? ExportRule { get; set; }
}

// Resource DTOs
public class ResourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string? Url { get; set; }
    public string? StoragePath { get; set; }
    public string? MimeType { get; set; }
    public long? SizeInBytes { get; set; }
    public string? SizeFormatted { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
    public int ViewCount { get; set; }
    public int ClickCount { get; set; }
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? DirectoryId { get; set; }
    public string DirectoryName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    /// <summary>Used server-side for IDOR guard. Not exposed to the client in API responses.</summary>
    public Guid OrganizationId { get; set; }
}


public class CreateResourceRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ResourceType Type { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // For links
    [MaxLength(2000)]
    public string? Url { get; set; }

    // For files (would be handled by file upload endpoint)
    public string? StoragePath { get; set; }
    public string? MimeType { get; set; }
    public long? SizeInBytes { get; set; }

    public bool IsPublic { get; set; } = false;

    public DateTime? ExpiresAt { get; set; }

    [Required]
    public Guid DirectoryId { get; set; }
}

public class UpdateResourceRequestDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

// Link DTOs
public class LinkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string LinkType { get; set; } = "Standard";
    public string? EmbedHtml { get; set; }
    public int DisplayOrder { get; set; }
    public int ClickCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool HasCodeLock { get; set; }
    public string? AnimationStyle { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLinkRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    [Url]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public string LinkType { get; set; } = "Standard";

    [MaxLength(5000)]
    public string? EmbedHtml { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsFeatured { get; set; } = false;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>4-digit PIN to protect this link.</summary>
    [MaxLength(4)]
    [MinLength(4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Code lock must be exactly 4 digits.")]
    public string? CodeLock { get; set; }

    [MaxLength(30)]
    public string? AnimationStyle { get; set; }
}

public class UpdateLinkRequestDto
{
    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    [Url]
    public string? Url { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public string? LinkType { get; set; }

    [MaxLength(5000)]
    public string? EmbedHtml { get; set; }

    public int? DisplayOrder { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsFeatured { get; set; }

    public bool? IsVisible { get; set; }

    public DateTime? ScheduledAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    /// <summary>Set to empty string to remove lock. 4-digit PIN otherwise.</summary>
    [MaxLength(4)]
    public string? CodeLock { get; set; }

    [MaxLength(30)]
    public string? AnimationStyle { get; set; }
}

public class ReorderLinksRequestDto
{
    [Required]
    public List<Guid> LinkIds { get; set; } = new();
}

public class UnlockLinkRequestDto
{
    [Required]
    [MaxLength(4)]
    public string Code { get; set; } = string.Empty;
}

// Profile Theme DTOs
public class ProfileThemeDto
{
    public Guid Id { get; set; }
    public string BackgroundType { get; set; } = "Gradient";
    public string BackgroundValue { get; set; } = string.Empty;
    public string ButtonStyle { get; set; } = "Rounded";
    public string ButtonColor { get; set; } = "#6366F1";
    public string ButtonTextColor { get; set; } = "#FFFFFF";
    public string? ButtonHoverColor { get; set; }
    public string FontFamily { get; set; } = "Inter";
    public string ProfileLayout { get; set; } = "List";
    public string SocialLinksPosition { get; set; } = "Bottom";
    public string? CustomCSS { get; set; }
    public bool HideBranding { get; set; }
}

public class UpdateProfileThemeDto
{
    public string? BackgroundType { get; set; }
    public string? BackgroundValue { get; set; }
    public string? ButtonStyle { get; set; }
    [MaxLength(7)]
    public string? ButtonColor { get; set; }
    [MaxLength(7)]
    public string? ButtonTextColor { get; set; }
    [MaxLength(7)]
    public string? ButtonHoverColor { get; set; }
    [MaxLength(100)]
    public string? FontFamily { get; set; }
    public string? ProfileLayout { get; set; }
    public string? SocialLinksPosition { get; set; }
    [MaxLength(5000)]
    public string? CustomCSS { get; set; }
    public bool? HideBranding { get; set; }
}

// Public Profile
public class PublicProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PublicPageSlug { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
    public ProfileThemeDto? Theme { get; set; }
    public List<PublicLinkDto> Links { get; set; } = new();
}

/// <summary>Slimmed-down link DTO for the public profile (hides code-lock value, admin-only fields).</summary>
public class PublicLinkDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string LinkType { get; set; } = "Standard";
    public string? EmbedHtml { get; set; }
    public int DisplayOrder { get; set; }
    public int ClickCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool HasCodeLock { get; set; }
    public string? AnimationStyle { get; set; }
}

