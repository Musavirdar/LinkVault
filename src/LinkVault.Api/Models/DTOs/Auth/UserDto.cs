namespace LinkVault.Api.Models.DTOs.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
