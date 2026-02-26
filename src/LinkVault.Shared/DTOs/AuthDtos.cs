namespace LinkVault.Shared.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    bool IsBusinessAccount = false,
    string? OrganizationName = null
);

public record LoginDto(string Email, string Password);

public record AuthResultDto
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public string? Error { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public bool Requires2FA { get; init; }
    public bool IsBusinessAccount { get; init; }

    public static AuthResultDto Successful(string token, Guid userId, string email, bool isBusiness) =>
        new() { Success = true, Token = token, UserId = userId, Email = email, IsBusinessAccount = isBusiness };

    public static AuthResultDto Failed(string error) =>
        new() { Success = false, Error = error };

    public static AuthResultDto TwoFactorRequired(Guid userId) =>
        new() { Success = true, Requires2FA = true, UserId = userId };
}

public record Setup2FAResponseDto(bool Success, string? SecretKey, string? QrCodeUri);

public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AccountType { get; init; }
    public string? OrganizationName { get; init; }
}

public record UpdateProfileDto(string? FirstName, string? LastName);
