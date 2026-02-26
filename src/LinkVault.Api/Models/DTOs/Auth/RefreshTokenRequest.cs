using System.ComponentModel.DataAnnotations;

namespace LinkVault.Api.Models.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
