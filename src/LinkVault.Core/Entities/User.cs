namespace LinkVault.Core.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public AccountType AccountType { get; set; } = AccountType.Personal;
    public Guid? OrganizationId { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecretKey { get; set; }
    public bool Is2FASetupComplete { get; set; }
    public string? SecurityStamp { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public enum AccountType
{
    Personal,
    Business
}
