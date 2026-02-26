namespace LinkVault.Api.Models.Entities;

public class LinkTag
{
    public Guid LinkId { get; set; }
    public Guid TagId { get; set; }
    
    public Link Link { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
