namespace LinkVault.Api.Models.DTOs.Directories;

public class DirectoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsPublic { get; set; }
    public int LinkCount { get; set; }
    public int SubDirectoryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
