using System.ComponentModel.DataAnnotations;

namespace LinkVaultApi.Dtos;

// Registration
public class RegisterRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OrganizationName { get; set; }

    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    /// <summary>True = Corporate/Team account, False = Individual account (default)</summary>
    public bool IsTeamAccount { get; set; } = false;
}

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// Login
public class LoginRequestDto
{
    [Required]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PreAuthToken { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string? Role { get; set; }
}

// 2FA
public class TwoFactorVerifyRequestDto
{
    [Required]
    public string PreAuthToken { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorSetupResponseDto
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string QrCodeImageBase64 { get; set; } = string.Empty;
    public string[] BackupCodes { get; set; } = Array.Empty<string>();
}

public class TwoFactorSetupVerifyDto
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

// Refresh Token
public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// SSO
public class SsoLoginRequestDto
{
    [Required]
    public string Provider { get; set; } = string.Empty; // Google, GitHub, LinkedIn

    [Required]
    public string Code { get; set; } = string.Empty;
}

// Invite
public class InviteRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public Guid? RoleId { get; set; }
}

public class AcceptInviteRequestDto
{
    [Required]
    public string InviteToken { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;
}

// User Profile
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Plan { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileRequestDto
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
