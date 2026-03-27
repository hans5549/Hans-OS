using HansOS.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<SportsDepartment> SportsDepartments => Set<SportsDepartment>();
    public DbSet<BankInitialBalance> BankInitialBalances => Set<BankInitialBalance>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<AnnualBudget> AnnualBudgets => Set<AnnualBudget>();
    public DbSet<DepartmentBudget> DepartmentBudgets => Set<DepartmentBudget>();
    public DbSet<BudgetItem> BudgetItems => Set<BudgetItem>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityGroup> ActivityGroups => Set<ActivityGroup>();
    public DbSet<ActivityExpense> ActivityExpenses => Set<ActivityExpense>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.RealName).HasMaxLength(100);
            e.Property(u => u.Avatar).HasMaxLength(500);
            e.Property(u => u.Desc).HasMaxLength(500);
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

        // SportsDepartment
        builder.Entity<SportsDepartment>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.Property(d => d.Note).HasMaxLength(500);
            e.HasIndex(d => d.Name).IsUnique();
        });

        // BankInitialBalance
        builder.Entity<BankInitialBalance>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.BankName).HasMaxLength(100).IsRequired();
            e.Property(b => b.InitialAmount).HasPrecision(18, 2);
            e.HasIndex(b => b.BankName).IsUnique();
        });

        // AnnualBudget
        builder.Entity<AnnualBudget>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Year).IsRequired();
            e.HasIndex(b => b.Year).IsUnique();
            e.Property(b => b.Status)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(b => b.Note).HasMaxLength(1000);
            e.Property(b => b.GrantedBudget).HasPrecision(18, 2);
        });

        // DepartmentBudget
        builder.Entity<DepartmentBudget>(e =>
        {
            e.HasKey(db => db.Id);
            e.HasOne(db => db.AnnualBudget)
             .WithMany(ab => ab.DepartmentBudgets)
             .HasForeignKey(db => db.AnnualBudgetId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(db => db.Department)
             .WithMany()
             .HasForeignKey(db => db.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(db => db.AllocatedAmount).HasPrecision(18, 2);
            e.HasIndex(db => new { db.AnnualBudgetId, db.DepartmentId }).IsUnique();
        });

        // BudgetItem
        builder.Entity<BudgetItem>(e =>
        {
            e.HasKey(bi => bi.Id);
            e.HasOne(bi => bi.DepartmentBudget)
             .WithMany(db => db.Items)
             .HasForeignKey(bi => bi.DepartmentBudgetId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(bi => bi.ActivityName).HasMaxLength(200).IsRequired();
            e.Property(bi => bi.ContentItem).HasMaxLength(200).IsRequired();
            e.Property(bi => bi.Amount).HasPrecision(18, 2);
            e.Property(bi => bi.ActualAmount).HasPrecision(18, 2);
            e.Property(bi => bi.Note).HasMaxLength(1000);
            e.Property(bi => bi.ActualNote).HasMaxLength(1000);
        });

        // BankTransaction
        builder.Entity<BankTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.BankName).HasMaxLength(100).IsRequired();
            e.Property(t => t.Description).HasMaxLength(200).IsRequired();
            e.Property(t => t.RequestingUnit).HasMaxLength(100);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Fee).HasPrecision(18, 2);
            e.Property(t => t.TransactionType)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasOne(t => t.Department)
             .WithMany()
             .HasForeignKey(t => t.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => new { t.BankName, t.TransactionDate });
        });

        // Activity
        builder.Entity<Activity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.Description).HasMaxLength(1000);
            e.Property(a => a.Month).IsRequired();
            e.Property(a => a.Year).IsRequired();

            e.HasOne(a => a.Department)
             .WithMany()
             .HasForeignKey(a => a.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.DepartmentId, a.Year, a.Month });
        });

        // ActivityGroup
        builder.Entity<ActivityGroup>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).HasMaxLength(200).IsRequired();

            e.HasOne(g => g.Activity)
             .WithMany(a => a.Groups)
             .HasForeignKey(g => g.ActivityId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityExpense
        builder.Entity<ActivityExpense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Description).HasMaxLength(200).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(1000);

            e.HasOne(x => x.Activity)
             .WithMany(a => a.Expenses)
             .HasForeignKey(x => x.ActivityId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Group)
             .WithMany(g => g.Expenses)
             .HasForeignKey(x => x.ActivityGroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.BudgetItem)
             .WithMany()
             .HasForeignKey(x => x.BudgetItemId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.ActivityId);
            e.HasIndex(x => x.ActivityGroupId);
            e.HasIndex(x => x.BudgetItemId);
        });
    }
}
