using HansOS.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.Avatar).HasMaxLength(512);
            entity.Property(user => user.HomePath).HasMaxLength(256);
            entity.Property(user => user.RealName).HasMaxLength(120);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasIndex(permission => permission.Code).IsUnique();
            entity.Property(permission => permission.Code).HasMaxLength(200);
            entity.Property(permission => permission.Description).HasMaxLength(500);
        });

        builder.Entity<Menu>(entity =>
        {
            entity.HasIndex(menu => menu.Path).IsUnique();
            entity.Property(menu => menu.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(menu => menu.ActivePath).HasMaxLength(256);
            entity.Property(menu => menu.Badge).HasMaxLength(64);
            entity.Property(menu => menu.BadgeType).HasMaxLength(32);
            entity.Property(menu => menu.BadgeVariant).HasMaxLength(64);
            entity.Property(menu => menu.ComponentKey).HasMaxLength(256);
            entity.Property(menu => menu.Icon).HasMaxLength(256);
            entity.Property(menu => menu.IframeSrc).HasMaxLength(512);
            entity.Property(menu => menu.Link).HasMaxLength(512);
            entity.Property(menu => menu.Name).HasMaxLength(128);
            entity.Property(menu => menu.Path).HasMaxLength(256);
            entity.Property(menu => menu.PermissionCode).HasMaxLength(200);
            entity.Property(menu => menu.Redirect).HasMaxLength(256);
            entity.Property(menu => menu.TitleKey).HasMaxLength(256);
            entity.HasOne(menu => menu.Parent)
                .WithMany(menu => menu.Children)
                .HasForeignKey(menu => menu.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(mapping => new { mapping.RoleId, mapping.PermissionId });
            entity.HasOne(mapping => mapping.Role)
                .WithMany()
                .HasForeignKey(mapping => mapping.RoleId);
            entity.HasOne(mapping => mapping.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(mapping => mapping.PermissionId);
        });

        builder.Entity<RoleMenu>(entity =>
        {
            entity.HasKey(mapping => new { mapping.RoleId, mapping.MenuId });
            entity.HasOne(mapping => mapping.Role)
                .WithMany()
                .HasForeignKey(mapping => mapping.RoleId);
            entity.HasOne(mapping => mapping.Menu)
                .WithMany(menu => menu.RoleMenus)
                .HasForeignKey(mapping => mapping.MenuId);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.Property(token => token.TokenHash).HasMaxLength(128);
            entity.Property(token => token.CreatedByIp).HasMaxLength(128);
            entity.Property(token => token.RevokedByIp).HasMaxLength(128);
            entity.Property(token => token.UserAgent).HasMaxLength(512);
        });
    }
}
