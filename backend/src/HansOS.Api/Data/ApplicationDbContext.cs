using System.Reflection;
using HansOS.Api.Data.Entities;
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
    public DbSet<ArticleBookmarkGroup> ArticleBookmarkGroups => Set<ArticleBookmarkGroup>();
    public DbSet<ArticleBookmark> ArticleBookmarks => Set<ArticleBookmark>();
    public DbSet<BudgetShareToken> BudgetShareTokens => Set<BudgetShareToken>();
    public DbSet<FinanceTask> FinanceTasks => Set<FinanceTask>();

    // Todo 模組
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();
    public DbSet<TodoTag> TodoTags => Set<TodoTag>();
    public DbSet<TodoChecklistItem> TodoChecklistItems => Set<TodoChecklistItem>();
    public DbSet<TodoItemRelation> TodoItemRelations => Set<TodoItemRelation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
