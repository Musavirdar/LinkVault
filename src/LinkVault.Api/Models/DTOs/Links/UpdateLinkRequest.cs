namespace LinkVault.Api.Models.DTOs.Links;

public class UpdateLinkRequest
{
    public string? OriginalUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? DirectoryId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? Tags { get; set; }
}
