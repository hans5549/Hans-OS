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
    public DbSet<PendingRemittance> PendingRemittances => Set<PendingRemittance>();
    public DbSet<FinanceAccount> FinanceAccounts => Set<FinanceAccount>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();
    public DbSet<FinanceTransaction> FinanceTransactions => Set<FinanceTransaction>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<BudgetShareToken> BudgetShareTokens => Set<BudgetShareToken>();
    public DbSet<TodoProject> TodoProjects => Set<TodoProject>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

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
            e.Property(t => t.Amount).HasPrecision(18, 0);
            e.Property(t => t.Fee).HasPrecision(18, 0);
            e.Property(t => t.TransactionType)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(t => t.ReceiptCollected).HasDefaultValue(false);

            e.HasOne(t => t.Department)
             .WithMany()
             .HasForeignKey(t => t.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Activity)
             .WithMany()
             .HasForeignKey(t => t.ActivityId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.PendingRemittance)
             .WithMany()
             .HasForeignKey(t => t.PendingRemittanceId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => new { t.BankName, t.TransactionDate });
        });

        // PendingRemittance
        builder.Entity<PendingRemittance>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Description).HasMaxLength(200).IsRequired();
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.SourceAccount).HasMaxLength(100).IsRequired();
            e.Property(r => r.TargetAccount).HasMaxLength(100).IsRequired();
            e.Property(r => r.RecipientName).HasMaxLength(100);
            e.Property(r => r.Note).HasMaxLength(1000);
            e.Property(r => r.Status)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasOne(r => r.Department)
             .WithMany()
             .HasForeignKey(r => r.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(r => r.ActivityExpense)
             .WithMany()
             .HasForeignKey(r => r.ActivityExpenseId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.DepartmentId);
            e.HasIndex(r => r.ActivityExpenseId);
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
             .WithMany(b => b.LinkedExpenses)
             .HasForeignKey(x => x.BudgetItemId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.ActivityId);
            e.HasIndex(x => x.ActivityGroupId);
            e.HasIndex(x => x.BudgetItemId);
        });

        // FinanceAccount — 個人記帳帳戶
        builder.Entity<FinanceAccount>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).IsRequired();
            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
            e.Property(a => a.AccountType)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(a => a.InitialBalance).HasPrecision(18, 2);
            e.Property(a => a.Icon).HasMaxLength(200);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.UserId, a.Name }).IsUnique();
        });

        // TransactionCategory — 交易分類（兩層自我參照）
        builder.Entity<TransactionCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Icon).HasMaxLength(200);
            e.Property(c => c.CategoryType)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Parent)
             .WithMany(c => c.Children)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(c => new { c.UserId, c.ParentId, c.Name }).IsUnique();
            e.HasIndex(c => c.ParentId);
        });

        // FinanceTransaction — 個人記帳交易記錄
        builder.Entity<FinanceTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.UserId).IsRequired();
            e.Property(t => t.TransactionType)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Note).HasMaxLength(500);

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Category)
             .WithMany()
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Account)
             .WithMany()
             .HasForeignKey(t => t.AccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ToAccount)
             .WithMany()
             .HasForeignKey(t => t.ToAccountId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(t => new { t.UserId, t.TransactionDate });
            e.HasIndex(t => new { t.UserId, t.CategoryId });
            e.HasIndex(t => new { t.UserId, t.AccountId });
        });

        // StockTransaction — 股票交易記錄
        builder.Entity<StockTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.UserId).IsRequired();
            e.Property(t => t.StockSymbol).HasMaxLength(20).IsRequired();
            e.Property(t => t.StockName).HasMaxLength(100).IsRequired();
            e.Property(t => t.TradeType)
             .HasConversion<string>()
             .HasMaxLength(10);
            e.Property(t => t.PricePerShare).HasPrecision(18, 4);
            e.Property(t => t.Commission).HasPrecision(18, 2);
            e.Property(t => t.Tax).HasPrecision(18, 2);
            e.Property(t => t.Note).HasMaxLength(500);

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => new { t.UserId, t.StockSymbol });
            e.HasIndex(t => new { t.UserId, t.TradeDate });
        });

        // BudgetShareToken — 部門預算分享連結（每部門一個永久 Token）
        builder.Entity<BudgetShareToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Token).HasMaxLength(100).IsRequired();
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.Permission)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasOne(t => t.Department)
             .WithMany()
             .HasForeignKey(t => t.DepartmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.DepartmentId).IsUnique();
        });

        // TodoProject — 待辦事項專案（清單）
        builder.Entity<TodoProject>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Color).HasMaxLength(20).HasDefaultValue("#3B82F6");

            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.UserId, p.Order });
        });

        // TodoItem — 待辦事項
        builder.Entity<TodoItem>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(500).IsRequired();
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Priority).HasConversion<int>();
            e.Property(t => t.Status).HasConversion<int>();

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Project)
             .WithMany(p => p.Items)
             .HasForeignKey(t => t.ProjectId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => new { t.UserId, t.Status });
            e.HasIndex(t => new { t.UserId, t.DueDate });
            e.HasIndex(t => new { t.UserId, t.ProjectId });
        });
    }
}
