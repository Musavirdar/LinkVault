namespace LinkVault.Api.Models.DTOs.Directories;

public class UpdateDirectoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool? IsPublic { get; set; }
}
