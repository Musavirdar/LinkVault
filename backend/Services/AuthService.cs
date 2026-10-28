using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Services;

public class AuthService : IAuthService
{
    private readonly LinkVaultDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IConfiguration _configuration;

    public AuthService(
        LinkVaultDbContext context,
        IJwtService jwtService,
        ITwoFactorService twoFactorService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _twoFactorService = twoFactorService;
        _configuration = configuration;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Validate password strength
        if (!IsPasswordStrong(request.Password))
        {
            throw new InvalidOperationException(
                "Password must be at least 8 characters and contain an uppercase letter, a number, and a special character.");
        }

        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already registered");
        }

        // Check if username exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("Username already taken");
        }

        // Create organization
        var orgName = !string.IsNullOrWhiteSpace(request.OrganizationName)
            ? request.OrganizationName
            : $"{request.Username}'s Space";
        var organization = new Organization
        {
            Name = orgName,
            Slug = GenerateSlug(orgName),
            AccountType = request.IsTeamAccount ? AccountType.Corporate : AccountType.Individual,
            SubscriptionTier = SubscriptionTier.Free,
            MaxAdmins = 3
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // ─── Seed system roles for this new org ───────────────────────────────
        var adminRole = new Role
        {
            Name = "Admin", Description = "Full access", Type = RoleType.Admin,
            CanUpload = true, CanUpdate = true, CanDelete = true,
            CanExport = true, CanShare = true, CanManageUsers = true,
            CanManageRoles = true, CanViewAuditLogs = true, CanManageSettings = true,
            OrganizationId = organization.Id, IsSystemRole = true
        };
        var managerRole = new Role
        {
            Name = "Manager", Description = "Can upload, update and share", Type = RoleType.Manager,
            CanUpload = true, CanUpdate = true, CanDelete = false,
            CanExport = true, CanShare = true, CanManageUsers = true,
            CanManageRoles = false, CanViewAuditLogs = true, CanManageSettings = false,
            OrganizationId = organization.Id, IsSystemRole = true
        };
        var memberRole = new Role
        {
            Name = "Member", Description = "Can view and upload", Type = RoleType.Member,
            CanUpload = true, CanUpdate = true, CanDelete = false,
            CanExport = true, CanShare = true, CanManageUsers = false,
            CanManageRoles = false, CanViewAuditLogs = false, CanManageSettings = false,
            OrganizationId = organization.Id, IsSystemRole = true
        };
        var viewerRole = new Role
        {
            Name = "Viewer", Description = "Read-only access", Type = RoleType.Viewer,
            CanUpload = false, CanUpdate = false, CanDelete = false,
            CanExport = false, CanShare = false, CanManageUsers = false,
            CanManageRoles = false, CanViewAuditLogs = false, CanManageSettings = false,
            OrganizationId = organization.Id, IsSystemRole = true
        };
        _context.Roles.AddRange(adminRole, managerRole, memberRole, viewerRole);
        await _context.SaveChangesAsync();
        // ─────────────────────────────────────────────────────────────────────

        // Hash password
        var passwordSalt = BCrypt.Net.BCrypt.GenerateSalt();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, passwordSalt);

        // Create user — first registrant is always Admin
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            DisplayName = request.DisplayName,
            Bio = request.Bio,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            OrganizationId = organization.Id,
            RoleId = adminRole.Id,   // ← Admin, so all permission claims are minted in JWT
            EmailVerified = true     // Auto-verify for simplicity
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            Message = "Registration successful"
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => 
                u.Email == request.EmailOrUsername || 
                u.Username == request.EmailOrUsername);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash!))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Check if 2FA is required
        if (user.IsTwoFactorEnabled)
        {
            var preAuthToken = _jwtService.GeneratePreAuthToken(user.Id);
            
            return new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                PreAuthToken = preAuthToken,
                RequiresTwoFactor = true,
                AccountType = user.Organization.AccountType.ToString(),
                Role = user.Role?.Name
            };
        }

        // Generate tokens
        var token = _jwtService.GenerateToken(user, user.Role);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            RequiresTwoFactor = false,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpiry(),
            AccountType = user.Organization.AccountType.ToString(),
            Role = user.Role?.Name
        };
    }

    public async Task<LoginResponseDto> LoginWithOAuthAsync(string provider, string providerId, string email, string name, string? avatarUrl)
    {
        // Find existing user by provider ID or Email
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                (provider == "google" && u.GoogleId == providerId) ||
                (provider == "github" && u.GitHubId == providerId) ||
                (provider == "linkedin" && u.LinkedInId == providerId) ||
                u.Email == email);

        if (user == null)
        {
            // Auto-register new user
            var orgName = $"{name.Split(' ')[0]}'s Space";
            var organization = new Organization
            {
                Name = orgName,
                Slug = GenerateSlug(orgName),
                AccountType = AccountType.Individual,
                SubscriptionTier = SubscriptionTier.Free,
                MaxAdmins = 3
            };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Seed system roles for new org
            var adminRole = SeedSystemRoles(organization.Id);
            _context.Roles.AddRange(adminRole.admin, adminRole.manager, adminRole.member, adminRole.viewer);
            await _context.SaveChangesAsync();

            user = new User
            {
                Email = email,
                Username = email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N")[..4],
                DisplayName = name,
                AvatarUrl = avatarUrl,
                OrganizationId = organization.Id,
                RoleId = adminRole.admin.Id, // First SSO user = Admin
                EmailVerified = true
            };

            // Link provider ID
            if (provider == "google") user.GoogleId = providerId;
            else if (provider == "github") user.GitHubId = providerId;
            else if (provider == "linkedin") user.LinkedInId = providerId;

            _context.Users.Add(user);
        }
        else
        {
            // Link provider if not linked yet but email matched
            bool updated = false;
            if (provider == "google" && string.IsNullOrEmpty(user.GoogleId)) { user.GoogleId = providerId; updated = true; }
            else if (provider == "github" && string.IsNullOrEmpty(user.GitHubId)) { user.GitHubId = providerId; updated = true; }
            else if (provider == "linkedin" && string.IsNullOrEmpty(user.LinkedInId)) { user.LinkedInId = providerId; updated = true; }
            
            if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(avatarUrl))
            {
                user.AvatarUrl = avatarUrl;
                updated = true;
            }

            if (!user.IsActive) throw new UnauthorizedAccessException("Account is disabled");

            user.LastLoginAt = DateTime.UtcNow;
            if (updated) _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();

        // Check if 2FA is required for existing users
        if (user.IsTwoFactorEnabled)
        {
            var preAuthToken = _jwtService.GeneratePreAuthToken(user.Id);
            return new LoginResponseDto
            {
                UserId = user.Id, Email = user.Email, Username = user.Username,
                DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl,
                PreAuthToken = preAuthToken, RequiresTwoFactor = true,
                AccountType = user.Organization?.AccountType.ToString() ?? "Individual",
                Role = user.Role?.Name
            };
        }

        // Make sure we have the role before generating token
        if (user.RoleId == null && user.OrganizationId != Guid.Empty)
        {
            user.RoleId = await GetDefaultRoleIdAsync(user.OrganizationId);
            if (user.RoleId != null)
            {
                user.Role = await _context.Roles.FindAsync(user.RoleId);
            }
        }

        var token = _jwtService.GenerateToken(user, user.Role);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new LoginResponseDto
        {
            UserId = user.Id, Email = user.Email, Username = user.Username,
            DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl,
            RequiresTwoFactor = false, Token = token, RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpiry(),
            AccountType = user.Organization?.AccountType.ToString() ?? "Individual",
            Role = user.Role?.Name
        };
    }

    public async Task<LoginResponseDto> VerifyTwoFactorAsync(TwoFactorVerifyRequestDto request)
    {
        var principal = _jwtService.ValidatePreAuthToken(request.PreAuthToken);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired pre-auth token");
        }

        var userId = Guid.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || !user.IsTwoFactorEnabled)
        {
            throw new UnauthorizedAccessException("2FA not enabled for this user");
        }

        // Verify TOTP code
        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret!, request.Code))
        {
            // Check backup codes
            var backupCodes = user.TwoFactorBackupCodes?.Split(',');
            if (backupCodes == null || !_twoFactorService.VerifyBackupCode(backupCodes, request.Code))
            {
                throw new UnauthorizedAccessException("Invalid 2FA code");
            }
        }

        // Generate tokens
        var token = _jwtService.GenerateToken(user, user.Role);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            RequiresTwoFactor = false,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpiry(),
            AccountType = user.Organization.AccountType.ToString(),
            Role = user.Role?.Name
        };
    }

    public async Task<TwoFactorSetupResponseDto> SetupTwoFactorAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var secretKey = _twoFactorService.GenerateSecretKey();
        var qrCodeUri = _twoFactorService.GenerateQrCodeUri(secretKey, user.Username, "LinkVault");
        var qrCodeImage = _twoFactorService.GenerateQrCodeImageBase64(qrCodeUri);
        var backupCodes = _twoFactorService.GenerateBackupCodes();

        // Store secret temporarily (will be confirmed after verification)
        user.TwoFactorSecret = secretKey;
        user.TwoFactorBackupCodes = string.Join(",", backupCodes);
        await _context.SaveChangesAsync();

        return new TwoFactorSetupResponseDto
        {
            SecretKey = secretKey,
            QrCodeUri = qrCodeUri,
            QrCodeImageBase64 = qrCodeImage,
            BackupCodes = backupCodes
        };
    }

    public async Task<bool> VerifyTwoFactorSetupAsync(Guid userId, TwoFactorSetupVerifyDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TwoFactorSecret == null)
        {
            return false;
        }

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, request.Code))
        {
            return false;
        }

        user.IsTwoFactorEnabled = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DisableTwoFactorAsync(Guid userId, string code)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsTwoFactorEnabled || user.TwoFactorSecret == null)
        {
            return false;
        }

        if (!_twoFactorService.VerifyCode(user.TwoFactorSecret, code))
        {
            return false;
        }

        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorBackupCodes = null;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u!.Organization)
            .Include(rt => rt.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.IsActive);

        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Generate new tokens
        var token = _jwtService.GenerateToken(refreshToken.User, refreshToken.User.Role);
        var newRefreshToken = await CreateRefreshTokenAsync(refreshToken.User.Id);

        // Revoke old refresh token
        refreshToken.IsUsed = true;
        await _context.SaveChangesAsync();

        return new RefreshTokenResponseDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            ExpiresAt = _jwtService.GetTokenExpiry()
        };
    }

    public async Task<bool> RevokeRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (token == null)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "User logout";
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return MapToProfileDto(user);
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;
        
        if (request.Bio != null)
            user.Bio = request.Bio;
        
        if (request.AvatarUrl != null)
            user.AvatarUrl = request.AvatarUrl;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.PasswordHash == null)
        {
            return false;
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        var passwordSalt = BCrypt.Net.BCrypt.GenerateSalt();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, passwordSalt);
        user.PasswordSalt = passwordSalt;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "User logout";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    // Helper methods
    private bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = _jwtService.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = userId
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    private async Task<Guid?> GetDefaultRoleIdAsync(Guid organizationId)
    {
        var defaultRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.Type == RoleType.Member);
        
        return defaultRole?.Id;
    }

    private static (Role admin, Role manager, Role member, Role viewer) SeedSystemRoles(Guid orgId)
    {
        var admin = new Role
        {
            Name = "Admin", Description = "Full access", Type = RoleType.Admin,
            CanUpload = true, CanUpdate = true, CanDelete = true,
            CanExport = true, CanShare = true, CanManageUsers = true,
            CanManageRoles = true, CanViewAuditLogs = true, CanManageSettings = true,
            OrganizationId = orgId, IsSystemRole = true
        };
        var manager = new Role
        {
            Name = "Manager", Description = "Can upload, update and share", Type = RoleType.Manager,
            CanUpload = true, CanUpdate = true, CanDelete = false,
            CanExport = true, CanShare = true, CanManageUsers = true,
            CanManageRoles = false, CanViewAuditLogs = true, CanManageSettings = false,
            OrganizationId = orgId, IsSystemRole = true
        };
        var member = new Role
        {
            Name = "Member", Description = "Can view and upload", Type = RoleType.Member,
            CanUpload = true, CanUpdate = true, CanDelete = false,
            CanExport = true, CanShare = true, CanManageUsers = false,
            CanManageRoles = false, CanViewAuditLogs = false, CanManageSettings = false,
            OrganizationId = orgId, IsSystemRole = true
        };
        var viewer = new Role
        {
            Name = "Viewer", Description = "Read-only access", Type = RoleType.Viewer,
            CanUpload = false, CanUpdate = false, CanDelete = false,
            CanExport = false, CanShare = false, CanManageUsers = false,
            CanManageRoles = false, CanViewAuditLogs = false, CanManageSettings = false,
            OrganizationId = orgId, IsSystemRole = true
        };
        return (admin, manager, member, viewer);
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    private static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
        var hasUpper = System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]");
        var hasDigit = System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]");
        var hasSpecial = System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        return hasUpper && hasDigit && hasSpecial;
    }

    private UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString(),
            AccountType = user.Organization.AccountType.ToString(),
            Role = user.Role?.Name,
            Plan = user.Organization.SubscriptionTier.ToString(),
            IsTwoFactorEnabled = user.IsTwoFactorEnabled,
            CreatedAt = user.CreatedAt
        };
    }
}
