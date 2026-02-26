using System.ComponentModel.DataAnnotations;

namespace LinkVault.Api.Models.DTOs.Links;

public class CreateLinkRequest
{
    [Required]
    [Url]
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? CustomShortCode { get; set; }
    
    [MaxLength(256)]
    public string? Title { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }
    
    public Guid? DirectoryId { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    public List<string>? Tags { get; set; }
}
