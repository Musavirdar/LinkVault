namespace LinkVault.Api.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public AccountType AccountType { get; set; } = AccountType.Individual;
    public bool IsActive { get; set; } = true;

    // 2FA
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public bool Is2FASetupComplete { get; set; } = false;

    // SSO
    public string? SsoProvider { get; set; }   // "Google", "GitHub", "LinkedIn"
    public string? SsoSubject { get; set; }    // Provider's user ID

    // Organization membership (for Corporate accounts)
    public Guid? OrganizationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Organization? Organization { get; set; }
    public ICollection<Link> Links { get; set; } = new List<Link>();
    public ICollection<UserDirectory> UserDirectories { get; set; } = new List<UserDirectory>();
    public ICollection<FileItem> Files { get; set; } = new List<FileItem>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public Subscription? Subscription { get; set; }
}

public enum AccountType
{
    Individual = 0,
    Corporate = 1
}
