using LinkVaultApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkVaultApi.Data;

public class LinkVaultDbContext : DbContext
{
    public LinkVaultDbContext(DbContextOptions<LinkVaultDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Models.Directory> Directories { get; set; } = null!;
    public DbSet<Resource> Resources { get; set; } = null!;
    public DbSet<Link> Links { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<ProfileTheme> ProfileThemes { get; set; } = null!;
    public DbSet<FileVersion> FileVersions { get; set; } = null!;
    public DbSet<ShareLink> ShareLinks { get; set; } = null!;
    public DbSet<ClickEvent> ClickEvents { get; set; } = null!;
    public DbSet<Transfer> Transfers { get; set; } = null!;
    public DbSet<TransferFile> TransferFiles { get; set; } = null!;
    public DbSet<DownloadTicket> DownloadTickets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Organization indexes
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Slug)
            .IsUnique();

        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("\"GoogleId\" IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.GitHubId)
            .IsUnique()
            .HasFilter("\"GitHubId\" IS NOT NULL");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.LinkedInId)
            .IsUnique()
            .HasFilter("\"LinkedInId\" IS NOT NULL");

        // Public profile slug (unique, nullable)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.PublicPageSlug)
            .IsUnique()
            .HasFilter("\"PublicPageSlug\" IS NOT NULL");

        // AuditLog indexes for performance
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.OrganizationId, a.CreatedAt });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.UserId, a.CreatedAt });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityType, a.EntityId });

        // ChatMessage indexes
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(c => new { c.DirectoryId, c.CreatedAt });

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(c => new { c.OrganizationId, c.CreatedAt });

        // Resource indexes
        modelBuilder.Entity<Resource>()
            .HasIndex(r => new { r.DirectoryId, r.IsActive });

        modelBuilder.Entity<Resource>()
            .HasIndex(r => new { r.OrganizationId, r.Type });

        // Link indexes
        modelBuilder.Entity<Link>()
            .HasIndex(l => new { l.UserId, l.DisplayOrder });

        // Link scheduling index for filtering visible links
        modelBuilder.Entity<Link>()
            .HasIndex(l => new { l.UserId, l.LinkType });

        // Attendance: unique per user per day
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.UserId, a.Date })
            .IsUnique();

        // ProfileTheme: one-to-one with User
        modelBuilder.Entity<ProfileTheme>()
            .HasOne(pt => pt.User)
            .WithOne(u => u.ProfileTheme)
            .HasForeignKey<ProfileTheme>(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ShareLink: unique token
        modelBuilder.Entity<ShareLink>()
            .HasIndex(s => s.Token)
            .IsUnique();

        modelBuilder.Entity<ShareLink>()
            .HasIndex(s => s.OrganizationId);

        // DownloadTicket: indexes
        modelBuilder.Entity<DownloadTicket>()
            .HasIndex(dt => dt.Token)
            .IsUnique();

        modelBuilder.Entity<DownloadTicket>()
            .HasIndex(dt => new { dt.ExpiresAtUtc, dt.IsUsed });

        modelBuilder.Entity<DownloadTicket>()
            .HasIndex(dt => dt.ResourceId);

        // Resource: nullable directory relationship
        modelBuilder.Entity<Resource>()
            .HasOne(r => r.Directory)
            .WithMany(d => d.Resources)
            .HasForeignKey(r => r.DirectoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ClickEvent: time-series queries
        modelBuilder.Entity<ClickEvent>()
            .HasIndex(c => new { c.LinkId, c.Timestamp });
        modelBuilder.Entity<ClickEvent>()
            .HasIndex(c => new { c.ResourceId, c.Timestamp });

        // FileVersion: resource lookup
        modelBuilder.Entity<FileVersion>()
            .HasIndex(fv => new { fv.ResourceId, fv.VersionNumber });

        // Transfer: token lookup + expiry cleanup
        modelBuilder.Entity<Transfer>()
            .HasIndex(t => t.Token)
            .IsUnique();
        modelBuilder.Entity<Transfer>()
            .HasIndex(t => new { t.IsActive, t.ExpiresAt });

        // Directory self-referencing relationship
        modelBuilder.Entity<Models.Directory>()
            .HasOne(d => d.ParentDirectory)
            .WithMany(d => d.SubDirectories)
            .HasForeignKey(d => d.ParentDirectoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed system organisation (required for Role FK)
        modelBuilder.Entity<Organization>().HasData(new Organization
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "System",
            Slug = "system",
            AccountType = AccountType.Individual,
            SubscriptionTier = SubscriptionTier.Free
        });

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Type = RoleType.Admin,
                Description = "Full access to all features",
                CanUpload = true,
                CanUpdate = true,
                CanDelete = true,
                CanExport = true,
                CanShare = true,
                CanManageUsers = true,
                CanManageRoles = true,
                CanViewAuditLogs = true,
                CanManageSettings = true,
                OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IsSystemRole = true
            },
            new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Manager",
                Type = RoleType.Manager,
                Description = "Can manage resources and users",
                CanUpload = true,
                CanUpdate = true,
                CanDelete = true,
                CanExport = true,
                CanShare = true,
                CanManageUsers = true,
                CanManageRoles = false,
                CanViewAuditLogs = true,
                CanManageSettings = false,
                OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IsSystemRole = true
            },
            new Role
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Member",
                Type = RoleType.Member,
                Description = "Standard member access",
                CanUpload = true,
                CanUpdate = true,
                CanDelete = false,
                CanExport = true,
                CanShare = true,
                CanManageUsers = false,
                CanManageRoles = false,
                CanViewAuditLogs = false,
                CanManageSettings = false,
                OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IsSystemRole = true
            },
            new Role
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Viewer",
                Type = RoleType.Viewer,
                Description = "View-only access",
                CanUpload = false,
                CanUpdate = false,
                CanDelete = false,
                CanExport = false,
                CanShare = false,
                CanManageUsers = false,
                CanManageRoles = false,
                CanViewAuditLogs = false,
                CanManageSettings = false,
                OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                IsSystemRole = true
            }
        );
    }
}
