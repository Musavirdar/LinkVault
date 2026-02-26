using System.ComponentModel.DataAnnotations;
using LinkVault.Api.Models.Entities;

namespace LinkVault.Api.Models.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Individual or Corporate</summary>
    public AccountType AccountType { get; set; } = AccountType.Individual;

    /// <summary>Required when AccountType is Corporate — creates a new organization.</summary>
    public string? OrganizationName { get; set; }
}
