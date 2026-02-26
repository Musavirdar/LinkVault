using System.ComponentModel.DataAnnotations;

namespace LinkVault.Api.Models.DTOs.Directories;

public class CreateDirectoryRequest
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public Guid? ParentId { get; set; }
    
    public bool IsPublic { get; set; } = false;
}
