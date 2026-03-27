using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.AnnualBudget;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HansOS.Api.Services;

public class AnnualBudgetService(
    ApplicationDbContext db,
    ILogger<AnnualBudgetService> logger) : IAnnualBudgetService
{
    private const int MinYear = 2020;
    private const int MaxYear = 2100;

    private static readonly Dictionary<BudgetStatus, BudgetStatus[]> AllowedTransitions = new()
    {
        [BudgetStatus.Draft] = [BudgetStatus.Submitted],
        [BudgetStatus.Submitted] = [BudgetStatus.Approved, BudgetStatus.Draft],
        [BudgetStatus.Approved] = [BudgetStatus.Settled],
        [BudgetStatus.Settled] = [],
    };

    public async Task<AnnualBudgetOverviewResponse> GetOverviewAsync(
        int year, CancellationToken ct = default)
    {
        ValidateYear(year);
        var budget = await EnsureBudgetAsync(year, ct);
        var departments = await GetDepartmentSummariesAsync(budget.Id, ct);
        return CreateOverviewResponse(budget, departments);
    }

    public async Task UpdateStatusAsync(int year, string status, CancellationToken ct = default)
    {
        ValidateYear(year);

        if (!Enum.TryParse<BudgetStatus>(status, ignoreCase: true, out var newStatus))
            throw new ArgumentException("無效的狀態值");

        var budget = await db.AnnualBudgets.FirstOrDefaultAsync(b => b.Year == year, ct)
            ?? throw new KeyNotFoundException($"{year} 年度預算不存在");

        ValidateStatusTransition(budget.Status, newStatus);

        logger.LogInformation(
            "預算狀態變更: Year={Year}, {OldStatus} → {NewStatus}",
            year, budget.Status, newStatus);

        budget.Status = newStatus;
        budget.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BudgetItemResponse>> GetDepartmentItemsAsync(
        int year, Guid departmentId, CancellationToken ct = default)
    {
        ValidateYear(year);
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        return await GetBudgetItemsAsync(deptBudget.Id, ct);
    }

    public async Task<List<BudgetItemResponse>> SaveDepartmentItemsAsync(
        int year, Guid departmentId, SaveBudgetItemsRequest request, CancellationToken ct = default)
    {
        ValidateYear(year);
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        var existingItems = await db.BudgetItems
            .Where(i => i.DepartmentBudgetId == deptBudget.Id)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        RemoveDeletedItems(existingItems, request.Items);
        UpsertBudgetItems(existingItems, request.Items, deptBudget.Id, now);
        deptBudget.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        // 若核定總預算已設定，重新計算各部門核定預算
        await RecalculateAllocationsAsync(deptBudget.AnnualBudgetId, ct);

        return await GetBudgetItemsAsync(deptBudget.Id, ct);
    }

    public async Task<AnnualBudgetOverviewResponse> UpdateGrantedBudgetAsync(
        int year, decimal grantedBudget, CancellationToken ct = default)
    {
        ValidateYear(year);
        var budget = await EnsureBudgetAsync(year, ct);

        budget.GrantedBudget = grantedBudget;
        budget.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await RecalculateAllocationsAsync(budget.Id, ct);

        var departments = await GetDepartmentSummariesAsync(budget.Id, ct);
        return CreateOverviewResponse(budget, departments);
    }

    /// <summary>確保年度預算存在，並自動為所有部門建立 DepartmentBudget</summary>
    private async Task<AnnualBudget> EnsureBudgetAsync(int year, CancellationToken ct)
    {
        var budget = await GetOrCreateBudgetAsync(year, ct);
        await EnsureDepartmentBudgetsAsync(budget.Id, ct);
        return budget;
    }

    private async Task<AnnualBudget> GetOrCreateBudgetAsync(int year, CancellationToken ct)
    {
        var budget = await db.AnnualBudgets.FirstOrDefaultAsync(b => b.Year == year, ct);
        if (budget is not null)
            return budget;

        var now = DateTime.UtcNow;
        budget = new AnnualBudget
        {
            Id = Guid.NewGuid(),
            Year = year,
            Status = BudgetStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.AnnualBudgets.Add(budget);

        try
        {
            await db.SaveChangesAsync(ct);
            return budget;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.AnnualBudgets.FirstAsync(b => b.Year == year, ct);
        }
    }

    private async Task EnsureDepartmentBudgetsAsync(Guid annualBudgetId, CancellationToken ct)
    {
        var allDeptIds = await db.SportsDepartments
            .AsNoTracking()
            .Select(d => d.Id)
            .ToListAsync(ct);

        var existingDeptIds = await db.DepartmentBudgets
            .AsNoTracking()
            .Where(d => d.AnnualBudgetId == annualBudgetId)
            .Select(d => d.DepartmentId)
            .ToListAsync(ct);

        var missingDeptIds = allDeptIds.Except(existingDeptIds).ToList();
        if (missingDeptIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var departmentId in missingDeptIds)
            db.DepartmentBudgets.Add(CreateDepartmentBudget(annualBudgetId, departmentId, now));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 並行請求可能觸發 unique(AnnualBudgetId, DepartmentId) 衝突，忽略即可
            db.ChangeTracker.Clear();
        }
    }

    /// <summary>查詢部門預算，確保年度預算已初始化</summary>
    private async Task<DepartmentBudget> FindDepartmentBudgetAsync(
        int year, Guid departmentId, CancellationToken ct)
    {
        await EnsureBudgetAsync(year, ct);

        return await db.DepartmentBudgets
            .Include(d => d.AnnualBudget)
            .FirstOrDefaultAsync(d => d.AnnualBudget.Year == year && d.DepartmentId == departmentId, ct)
            ?? throw new KeyNotFoundException($"{year} 年度中找不到部門 {departmentId} 的預算");
    }

    /// <summary>根據核定總預算按比例重算各部門核定預算</summary>
    private async Task RecalculateAllocationsAsync(Guid annualBudgetId, CancellationToken ct)
    {
        var budget = await db.AnnualBudgets.FindAsync([annualBudgetId], ct);
        if (budget?.GrantedBudget is not { } grantedBudget)
            return;

        var deptBudgets = await db.DepartmentBudgets
            .Where(d => d.AnnualBudgetId == annualBudgetId)
            .ToListAsync(ct);

        var deptTotals = await db.DepartmentBudgets
            .Where(d => d.AnnualBudgetId == annualBudgetId)
            .Select(d => new { d.Id, Total = d.Items.Sum(i => i.Amount) })
            .ToListAsync(ct);

        var totalBudget = deptTotals.Sum(d => d.Total);
        var totalMap = deptTotals.ToDictionary(d => d.Id, d => d.Total);

        if (totalBudget == 0m)
        {
            foreach (var dept in deptBudgets)
                dept.AllocatedAmount = 0m;
        }
        else
        {
            var now = DateTime.UtcNow;
            decimal allocatedSum = 0m;
            DepartmentBudget? largestDept = null;
            decimal largestBudget = -1m;

            foreach (var dept in deptBudgets)
            {
                var deptTotal = totalMap.GetValueOrDefault(dept.Id, 0m);
                var allocated = Math.Round(grantedBudget * deptTotal / totalBudget, 2, MidpointRounding.AwayFromZero);
                dept.AllocatedAmount = allocated;
                dept.UpdatedAt = now;
                allocatedSum += allocated;

                if (deptTotal > largestBudget)
                {
                    largestBudget = deptTotal;
                    largestDept = dept;
                }
            }

            // 將四捨五入差額加到最大預算部門
            var diff = grantedBudget - allocatedSum;
            if (diff != 0m && largestDept is not null)
                largestDept.AllocatedAmount += diff;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<List<BudgetItemResponse>> GetBudgetItemsAsync(Guid departmentBudgetId, CancellationToken ct)
        => await db.BudgetItems
            .AsNoTracking()
            .Where(i => i.DepartmentBudgetId == departmentBudgetId)
            .OrderBy(i => i.Sequence)
            .Select(i => new BudgetItemResponse(
                i.Id, i.Sequence, i.ActivityName, i.ContentItem,
                i.Amount, i.Note, i.ActualAmount, i.ActualNote))
            .ToListAsync(ct);

    private void RemoveDeletedItems(IEnumerable<BudgetItem> existingItems, IEnumerable<BudgetItemInput> items)
    {
        var incomingIds = items
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        var itemsToDelete = existingItems.Where(item => !incomingIds.Contains(item.Id)).ToList();
        db.BudgetItems.RemoveRange(itemsToDelete);
    }

    private void UpsertBudgetItems(
        IEnumerable<BudgetItem> existingItems,
        IEnumerable<BudgetItemInput> items,
        Guid departmentBudgetId,
        DateTime now)
    {
        var existingById = existingItems.ToDictionary(item => item.Id);
        foreach (var item in items)
        {
            if (!item.Id.HasValue)
            {
                db.BudgetItems.Add(CreateBudgetItem(item, departmentBudgetId, now));
                continue;
            }

            var existingItem = existingById.GetValueOrDefault(item.Id.Value)
                ?? throw new KeyNotFoundException($"預算項目 {item.Id.Value} 不存在");

            UpdateBudgetItem(existingItem, item, now);
        }
    }

    private static void UpdateBudgetItem(BudgetItem existingItem, BudgetItemInput item, DateTime now)
    {
        existingItem.Sequence = item.Sequence;
        existingItem.ActivityName = item.ActivityName;
        existingItem.ContentItem = item.ContentItem;
        existingItem.Amount = item.Amount;
        existingItem.Note = item.Note;
        existingItem.ActualAmount = item.ActualAmount;
        existingItem.ActualNote = item.ActualNote;
        existingItem.UpdatedAt = now;
    }

    private static DepartmentBudget CreateDepartmentBudget(Guid annualBudgetId, Guid departmentId, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            AnnualBudgetId = annualBudgetId,
            DepartmentId = departmentId,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static BudgetItem CreateBudgetItem(BudgetItemInput item, Guid departmentBudgetId, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            DepartmentBudgetId = departmentBudgetId,
            Sequence = item.Sequence,
            ActivityName = item.ActivityName,
            ContentItem = item.ContentItem,
            Amount = item.Amount,
            Note = item.Note,
            ActualAmount = item.ActualAmount,
            ActualNote = item.ActualNote,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private async Task<List<DepartmentBudgetSummaryResponse>> GetDepartmentSummariesAsync(
        Guid annualBudgetId,
        CancellationToken ct)
        => await db.DepartmentBudgets
            .AsNoTracking()
            .Where(d => d.AnnualBudgetId == annualBudgetId)
            .OrderBy(d => d.Department.Name)
            .Select(d => new DepartmentBudgetSummaryResponse(
                d.Id,
                d.DepartmentId,
                d.Department.Name,
                d.Items.Sum(i => i.Amount),
                d.Items.Sum(i => i.ActualAmount ?? 0m),
                d.AllocatedAmount,
                d.Items.Count))
            .ToListAsync(ct);

    private static AnnualBudgetOverviewResponse CreateOverviewResponse(
        AnnualBudget budget,
        List<DepartmentBudgetSummaryResponse> departments)
        => new(
            budget.Id,
            budget.Year,
            budget.Status.ToString(),
            budget.Note,
            departments.Sum(d => d.BudgetAmount),
            departments.Sum(d => d.ActualAmount),
            budget.GrantedBudget,
            departments);

    private static void ValidateYear(int year)
    {
        if (year < MinYear || year > MaxYear)
            throw new ArgumentException($"年度必須在 {MinYear} 至 {MaxYear} 之間");
    }

    private static void ValidateStatusTransition(BudgetStatus current, BudgetStatus target)
    {
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
            throw new ArgumentException($"無法從「{current}」轉換為「{target}」");
    }
}
