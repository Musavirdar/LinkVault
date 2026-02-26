using Microsoft.EntityFrameworkCore;
using LinkVault.Api.Models.Entities;

namespace LinkVault.Infrastructure.Data;

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
    public DbSet<LinkTag> LinkTags => Set<LinkTag>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<LinkClick> LinkClicks => Set<LinkClick>();

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
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.AccountType)
                  .HasConversion<string>();

            // User → Organization (many users belong to one org)
            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Members)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RefreshToken ──────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
        });

        // ── RBAC ──────────────────────────────────────────────────────────
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId, });
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
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.HasOne(l => l.User).WithMany(u => u.Links).HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.LinkDirectory).WithMany(d => d.Links).HasForeignKey(l => l.DirectoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LinkTag>(entity =>
        {
            entity.HasKey(lt => new { lt.LinkId, lt.TagId });
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
        });

        // ── UserDirectory ─────────────────────────────────────────────────
        modelBuilder.Entity<UserDirectory>(entity =>
        {
            entity.HasOne(d => d.ParentDirectory)
                  .WithMany(d => d.SubDirectories)
                  .HasForeignKey(d => d.ParentDirectoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Owner)
                  .WithMany(u => u.UserDirectories)
                  .HasForeignKey(d => d.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Organization)
                  .WithMany(o => o.Directories)
                  .HasForeignKey(d => d.OrganizationId)
                  .OnDelete(DeleteBehavior.Restrict);
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

        // ── Seed system-wide roles ────────────────────────────────────────
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var employeeRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Admin", Description = "Organization Administrator", IsSystemRole = true, CreatedAt = DateTime.UtcNow },
            new Role { Id = employeeRoleId, Name = "Employee", Description = "Organization Employee", IsSystemRole = true, CreatedAt = DateTime.UtcNow }
        );
    }
}

