using HansOS.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.RealName).HasMaxLength(100);
            e.Property(u => u.Avatar).HasMaxLength(500);
            e.Property(u => u.HomePath).HasMaxLength(200);
        });

        // Menu — self-referencing tree
        builder.Entity<Menu>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).HasMaxLength(100).IsRequired();
            e.Property(m => m.Path).HasMaxLength(300).IsRequired();
            e.Property(m => m.Component).HasMaxLength(300);
            e.Property(m => m.Redirect).HasMaxLength(300);
            e.Property(m => m.Title).HasMaxLength(200).IsRequired();
            e.Property(m => m.Icon).HasMaxLength(200);
            e.Property(m => m.ActiveIcon).HasMaxLength(200);
            e.Property(m => m.Authority).HasMaxLength(500);
            e.Property(m => m.Badge).HasMaxLength(50);
            e.Property(m => m.BadgeType).HasMaxLength(50);
            e.Property(m => m.BadgeVariants).HasMaxLength(50);
            e.Property(m => m.Link).HasMaxLength(500);
            e.Property(m => m.IframeSrc).HasMaxLength(500);

            e.HasOne(m => m.Parent)
             .WithMany(m => m.Children)
             .HasForeignKey(m => m.ParentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => m.ParentId);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(t => t.UserId).IsRequired();

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.TokenHash);
            e.HasIndex(t => t.UserId);
        });

        // RoleMenu — composite key
        builder.Entity<RoleMenu>(e =>
        {
            e.HasKey(rm => new { rm.RoleId, rm.MenuId });

            e.HasOne(rm => rm.Menu)
             .WithMany(m => m.RoleMenus)
             .HasForeignKey(rm => rm.MenuId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(rm => rm.RoleId);
        });
    }
}
