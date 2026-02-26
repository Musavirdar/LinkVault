namespace LinkVault.Api.Models.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public User User { get; set; } = null!;
    public ICollection<LinkTag> LinkTags { get; set; } = new List<LinkTag>();
}
