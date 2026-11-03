using System.Security.Cryptography;
using System.Text;
using LinkVaultApi.Data;
using LinkVaultApi.Dtos;
using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Services;

public class DemoSeedService : IDemoSeedService
{
    private readonly LinkVaultDbContext _dbContext;
    private readonly IJwtService _jwtService;
    private readonly IStorageProvider _storageProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoSeedService> _logger;

    public const string DemoOrgSlug = "acme-corp-demo";
    public const string DemoAdminUsername = "demo_admin";
    public const string DemoMemberUsername = "demo_member";

    public DemoSeedService(
        LinkVaultDbContext dbContext,
        IJwtService jwtService,
        IStorageProvider storageProvider,
        IConfiguration configuration,
        ILogger<DemoSeedService> logger)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _storageProvider = storageProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsDemoModeEnabled()
    {
        var envVal = Environment.GetEnvironmentVariable("DEMO_MODE");
        if (string.Equals(envVal, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        return _configuration.GetValue<bool>("DemoMode", false);
    }

    public async Task EnsureDemoWorkspaceSeededAsync(CancellationToken cancellationToken = default)
    {
        // 1. Organization
        var org = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.Slug == DemoOrgSlug, cancellationToken);
        if (org == null)
        {
            org = new Organization
            {
                Name = "Acme Corp Demo Workspace",
                Slug = DemoOrgSlug,
                AccountType = AccountType.Corporate,
                SubscriptionTier = SubscriptionTier.Pro,
                IsActive = true,
                StorageLimitBytes = 100L * 1024 * 1024 * 1024, // 100 GB
                StorageUsedBytes = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Roles
        var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.Type == RoleType.Admin, cancellationToken);
        if (adminRole == null)
        {
            adminRole = new Role
            {
                OrganizationId = org.Id,
                Name = "Admin",
                Description = "Workspace Administrator with full privileges",
                Type = RoleType.Admin,
                CanUpload = true,
                CanUpdate = true,
                CanDelete = true,
                CanExport = true,
                CanShare = true,
                CanManageUsers = true,
                CanManageRoles = true,
                CanViewAuditLogs = true,
                CanManageSettings = true
            };
            _dbContext.Roles.Add(adminRole);
        }

        var memberRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.Type == RoleType.Member, cancellationToken);
        if (memberRole == null)
        {
            memberRole = new Role
            {
                OrganizationId = org.Id,
                Name = "Member",
                Description = "Standard workspace member",
                Type = RoleType.Member,
                CanUpload = true,
                CanUpdate = true,
                CanDelete = false,
                CanExport = true,
                CanShare = true,
                CanManageUsers = false,
                CanManageRoles = false,
                CanViewAuditLogs = false,
                CanManageSettings = false
            };
            _dbContext.Roles.Add(memberRole);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Users
        var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == DemoAdminUsername && u.OrganizationId == org.Id, cancellationToken);
        if (adminUser == null)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            adminUser = new User
            {
                OrganizationId = org.Id,
                RoleId = adminRole.Id,
                Email = "demo-admin@linkvault.demo",
                Username = DemoAdminUsername,
                DisplayName = "Demo Administrator",
                PasswordSalt = salt,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoAdmin2026!", salt),
                IsActive = true,
                EmailVerified = true,
                IsTwoFactorEnabled = false,
                Status = UserStatus.Online,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(adminUser);
        }

        var memberUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == DemoMemberUsername && u.OrganizationId == org.Id, cancellationToken);
        if (memberUser == null)
        {
            var salt = BCrypt.Net.BCrypt.GenerateSalt();
            memberUser = new User
            {
                OrganizationId = org.Id,
                RoleId = memberRole.Id,
                Email = "demo-member@linkvault.demo",
                Username = DemoMemberUsername,
                DisplayName = "Demo Member",
                PasswordSalt = salt,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoMember2026!", salt),
                IsActive = true,
                EmailVerified = true,
                IsTwoFactorEnabled = false,
                Status = UserStatus.Online,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(memberUser);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Five Folders
        var folderNames = new[]
        {
            ("Invoices & Receipts", "Financial records, invoices, and billing receipts"),
            ("Engineering & Specs", "Technical documentation, architecture, and code specs"),
            ("Architecture & Designs", "System designs, cloud diagrams, and UI mockups"),
            ("Compliance & Security", "Security policies, credentials, and audit artifacts"),
            ("Archive & Backups", "Historical workspace assets and archives")
        };

        var folders = new Dictionary<string, Models.Directory>();
        foreach (var (folderName, desc) in folderNames)
        {
            var dir = await _dbContext.Directories.FirstOrDefaultAsync(d => d.OrganizationId == org.Id && d.Name == folderName, cancellationToken);
            if (dir == null)
            {
                dir = new Models.Directory
                {
                    OrganizationId = org.Id,
                    CreatedById = adminUser.Id,
                    Name = folderName,
                    Description = desc,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Directories.Add(dir);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            folders[folderName] = dir;
        }

        // 5. Fifteen Sample Files (including fake secret/db file for Safe Share)
        var sampleFiles = new List<(string Name, string Folder, string Content, string MimeType, FileType FileType)>
        {
            // CRITICAL TEST RESOURCE: Exposed Credentials triggering Safe Share Guard
            (
                "database_backup_config.env",
                "Compliance & Security",
                "# Acme Corp Production DB Credentials (FAKE DEMO)\nDB_HOST=prod-db.internal.linkvault.io\nDB_PORT=5432\nDB_USER=postgres\nPassword=SuperSecretP@ssw0rd!1234\nAWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE\nAPI_SECRET_KEY='sk_live_fake_demo_secret_token_abcdef123456'\n",
                "text/plain",
                FileType.Document
            ),
            (
                "security_audit_checklist.docx",
                "Compliance & Security",
                "# Security Audit Checklist Q4 2026\n1. Enforce 2FA for all privileged admin roles.\n2. Review AWS IAM least-privilege policies.\n3. Validate zero-trust DLP share links.\n",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                FileType.Document
            ),
            (
                "incident_postmortem_oct2026.md",
                "Compliance & Security",
                "# Incident Postmortem: Cache Latency Spike\nDate: Oct 2026\nImpact: 12ms redis degradation resolved via autoscaling.\n",
                "text/markdown",
                FileType.Document
            ),
            (
                "vendor_invoice_1099.pdf",
                "Invoices & Receipts",
                "%PDF-1.4\nInvoice #INV-2026-1099\nVendor: Cloud Infrastructure Providers Inc.\nAmount Due: $1,450.00 USD\nStatus: Paid\n",
                "application/pdf",
                FileType.Document
            ),
            (
                "q3_financial_report.pdf",
                "Invoices & Receipts",
                "%PDF-1.4\nAcme Corp Q3 Financial Statement\nGross Revenue: $4,200,000\nNet Margin: 28%\n",
                "application/pdf",
                FileType.Document
            ),
            (
                "customer_contracts_standard_template.pdf",
                "Invoices & Receipts",
                "%PDF-1.4\nMaster Services Agreement Template\nStandard terms for corporate enterprise licensing.\n",
                "application/pdf",
                FileType.Document
            ),
            (
                "aws_cloud_architecture_2026.png",
                "Architecture & Designs",
                "PNG_FAKE_IMAGE_DATA_CLOUD_TOPOLOGY_DIAGRAM",
                "image/png",
                FileType.Image
            ),
            (
                "system_diagram_overview.png",
                "Architecture & Designs",
                "PNG_FAKE_IMAGE_DATA_SYSTEM_MICROSERVICES_GRAPH",
                "image/png",
                FileType.Image
            ),
            (
                "mobile_app_redesign_mockup.png",
                "Architecture & Designs",
                "PNG_FAKE_IMAGE_DATA_FIGMA_MOBILE_UI_EXPORT",
                "image/png",
                FileType.Image
            ),
            (
                "kubernetes_cluster_manifest.yaml",
                "Engineering & Specs",
                "apiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: linkvault-api\nspec:\n  replicas: 3\n",
                "text/yaml",
                FileType.Document
            ),
            (
                "product_roadmap_2026_q4.xlsx",
                "Engineering & Specs",
                "PK\x03\x04 Roadmap Q4: AI Agent Automation, Multi-Region Vault Storage, Enterprise SAML",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileType.Document
            ),
            (
                "database_schema_v2.sql",
                "Engineering & Specs",
                "CREATE TABLE workspace_resources (\n  id UUID PRIMARY KEY,\n  name VARCHAR(200) NOT NULL\n);\n",
                "text/plain",
                FileType.Document
            ),
            (
                "api_specification_v1.json",
                "Engineering & Specs",
                "{\n  \"openapi\": \"3.0.0\",\n  \"info\": { \"title\": \"LinkVault API\", \"version\": \"1.0.0\" }\n}\n",
                "application/json",
                FileType.Document
            ),
            (
                "brand_assets_vector_kit.zip",
                "Archive & Backups",
                "PK\x03\x04 ZIP archive containing SVG logos, color palettes, and typography tokens",
                "application/zip",
                FileType.Archive
            ),
            (
                "meeting_notes_executive_sync.txt",
                "Archive & Backups",
                "Executive sync notes: Oct 2026. Goals: Complete Hackathon submission with verified Meta models.",
                "text/plain",
                FileType.Document
            )
        };

        foreach (var (name, folder, content, mime, fileType) in sampleFiles)
        {
            var res = await _dbContext.Resources.FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.Name == name, cancellationToken);
            if (res == null)
            {
                var targetDir = folders.TryGetValue(folder, out var d) ? d : null;
                var storageKey = $"demo/{org.Id}/{Guid.NewGuid()}_{name}";
                var bytes = Encoding.UTF8.GetBytes(content);

                try
                {
                    using var ms = new MemoryStream(bytes);
                    await _storageProvider.SaveAsync(ms, storageKey, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not write demo sample file {FileName} to storage provider; using direct key reference.", name);
                }

                res = new Resource
                {
                    OrganizationId = org.Id,
                    CreatedById = adminUser.Id,
                    DirectoryId = targetDir?.Id,
                    Name = name,
                    Type = ResourceType.File,
                    FileType = fileType,
                    MimeType = mime,
                    StoragePath = storageKey,
                    SizeInBytes = bytes.Length,
                    IsPublic = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Resources.Add(res);
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 6. Five Sample Links
        var sampleLinks = new[]
        {
            ("GitHub Repository", "https://github.com/linkvault/workspace", "Core source code repository and issue tracker", "github"),
            ("Figma Design System", "https://figma.com/@linkvault-design", "UI component library, Apple look tokens, and screen mockups", "figma"),
            ("Cloud Monitoring Dashboard", "https://console.cloud.google.com/monitoring", "Real-time metrics, slot consumption, and telemetry alerts", "cloud"),
            ("Internal API Documentation", "https://developer.linkvault.io/docs", "REST endpoints, SignalR hubs, and tool schemas", "docs"),
            ("Compliance Portal", "https://compliance.internal.linkvault.io", "SOC2 compliance policies and DLP audit records", "shield")
        };

        foreach (var (title, url, desc, icon) in sampleLinks)
        {
            var link = await _dbContext.Links.FirstOrDefaultAsync(l => l.OrganizationId == org.Id && l.Title == title, cancellationToken);
            if (link == null)
            {
                link = new Link
                {
                    OrganizationId = org.Id,
                    UserId = adminUser.Id,
                    Title = title,
                    Url = url,
                    Description = desc,
                    Icon = icon,
                    LinkType = LinkType.Standard,
                    IsActive = true,
                    IsVisible = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Links.Add(link);
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Demo workspace '{OrgName}' successfully verified and seeded.", org.Name);
    }

    public async Task<LoginResponseDto> LoginDemoUserAsync(string role = "Admin", CancellationToken cancellationToken = default)
    {
        await EnsureDemoWorkspaceSeededAsync(cancellationToken);

        var isAdmin = !string.Equals(role, "Member", StringComparison.OrdinalIgnoreCase);
        var targetUsername = isAdmin ? DemoAdminUsername : DemoMemberUsername;

        var user = await _dbContext.Users
            .Include(u => u.Organization)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == targetUsername, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"Demo user '{targetUsername}' could not be loaded.");
        }

        var token = _jwtService.GenerateToken(user, user.Role);
        var refreshTokenString = _jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            RequiresTwoFactor = false,
            Token = token,
            RefreshToken = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            AccountType = user.Organization.AccountType.ToString(),
            Role = user.Role?.Name ?? (isAdmin ? "Admin" : "Member")
        };
    }
}
