using LinkVault.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkVault.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Auth & Users
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Organizations & RBAC
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // Links
    public DbSet<Link> Links => Set<Link>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<LinkTag> LinkTags => Set<LinkTag>();
    public DbSet<LinkClick> LinkClicks => Set<LinkClick>();
    public DbSet<LinkDirectory> LinkDirectories => Set<LinkDirectory>();

    // Directories
    public DbSet<UserDirectory> Directories => Set<UserDirectory>();

    // Files
    public DbSet<FileItem> Files => Set<FileItem>();
    public DbSet<FileVersion> FileVersions => Set<FileVersion>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

    // DLP / Export
    public DbSet<ExportRequest> ExportRequests => Set<ExportRequest>();

    // Audit & Chat
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // Subscriptions
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.AccountType).HasConversion<string>();

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Members)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RefreshToken ──────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── RBAC ──────────────────────────────────────────────────────────
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
            entity.HasOne(ur => ur.Organization).WithMany().HasForeignKey(ur => ur.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            entity.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
        });

        // ── Links ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ShortCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OriginalUrl).HasMaxLength(2048).IsRequired();
            entity.HasOne(e => e.User).WithMany(u => u.Links).HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LinkDirectory).WithMany(d => d.Links).HasForeignKey(e => e.DirectoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LinkDirectory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Parent).WithMany(d => d.SubDirectories).HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<LinkTag>(entity => entity.HasKey(e => new { e.LinkId, e.TagId }));
        modelBuilder.Entity<LinkClick>(entity => entity.HasKey(e => e.Id));

        // ── UserDirectory ─────────────────────────────────────────────────
        modelBuilder.Entity<UserDirectory>(entity =>
        {
            entity.HasOne(d => d.ParentDirectory).WithMany(d => d.SubDirectories)
                  .HasForeignKey(d => d.ParentDirectoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Owner).WithMany(u => u.UserDirectories)
                  .HasForeignKey(d => d.OwnerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Organization).WithMany(o => o.Directories)
                  .HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Files ─────────────────────────────────────────────────────────
        modelBuilder.Entity<FileItem>(entity =>
        {
            entity.HasIndex(e => e.StoragePath).IsUnique();
            entity.HasOne(f => f.UploadedBy).WithMany(u => u.Files).HasForeignKey(f => f.UploadedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(f => f.Directory).WithMany().HasForeignKey(f => f.DirectoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShareLink>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(s => s.File).WithMany(f => f.ShareLinks).HasForeignKey(s => s.FileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExportRequest>(entity =>
        {
            entity.HasOne(er => er.FileItem).WithMany(f => f.ExportRequests).HasForeignKey(er => er.FileItemId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(er => er.RequestedBy).WithMany().HasForeignKey(er => er.RequestedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(er => er.ReviewedBy).WithMany().HasForeignKey(er => er.ReviewedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AuditLog ──────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.Organization).WithMany(o => o.AuditLogs).HasForeignKey(a => a.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ChatMessage ───────────────────────────────────────────────────
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(c => c.Sender).WithMany(u => u.ChatMessages).HasForeignKey(c => c.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Directory).WithMany().HasForeignKey(c => c.DirectoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Organization).WithMany(o => o.ChatMessages).HasForeignKey(c => c.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Subscription ──────────────────────────────────────────────────
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasOne(s => s.User).WithOne(u => u.Subscription).HasForeignKey<Subscription>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Organization).WithOne(o => o.Subscription).HasForeignKey<Subscription>(s => s.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Invitation ────────────────────────────────────────────────────
        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasIndex(i => i.Token).IsUnique();
            entity.HasOne(i => i.Organization).WithMany(o => o.Invitations).HasForeignKey(i => i.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(i => i.InvitedBy).WithMany().HasForeignKey(i => i.InvitedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Seed system roles ─────────────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin", Description = "Organization Administrator", IsSystemRole = true, CreatedAt = DateTime.UtcNow },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Employee", Description = "Organization Employee", IsSystemRole = true, CreatedAt = DateTime.UtcNow }
        );
    }
}
