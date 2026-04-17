using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Activities;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class ActivityService(ApplicationDbContext db) : IActivityService
{
    public async Task<List<ActivitySummaryResponse>> GetListAsync(
        int year, int? month, Guid? departmentId, CancellationToken ct = default)
    {
        var query = db.Activities
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Expenses)
            .Include(a => a.Groups)
            .Where(a => a.Year == year);

        if (month.HasValue)
            query = query.Where(a => a.Month == month.Value);

        if (departmentId.HasValue)
            query = query.Where(a => a.DepartmentId == departmentId.Value);

        return await query
            .OrderBy(a => a.Month)
            .ThenBy(a => a.Sequence)
            .ThenBy(a => a.Name)
            .Select(a => new ActivitySummaryResponse(
                a.Id,
                a.Name,
                a.Description,
                a.DepartmentId,
                a.Department.Name,
                a.Year,
                a.Month,
                a.Sequence,
                a.Expenses.Sum(e => e.Amount),
                a.Groups.Count,
                a.Expenses.Count))
            .ToListAsync(ct);
    }

    public async Task<List<MonthSummaryResponse>> GetMonthSummariesAsync(
        int year, Guid? departmentId, CancellationToken ct = default)
    {
        var query = db.Activities
            .AsNoTracking()
            .Where(a => a.Year == year);

        if (departmentId.HasValue)
            query = query.Where(a => a.DepartmentId == departmentId.Value);

        var activities = await query
            .Select(a => new
            {
                a.Month,
                Total = a.Expenses.Sum(e => e.Amount),
            })
            .ToListAsync(ct);

        return activities
            .GroupBy(a => a.Month)
            .Select(g => new MonthSummaryResponse(
                g.Key,
                g.Count(),
                g.Sum(a => a.Total)))
            .OrderBy(s => s.Month)
            .ToList();
    }

    public async Task<ActivityDetailResponse> GetDetailAsync(
        Guid id, CancellationToken ct = default)
    {
        var activity = await db.Activities
            .AsNoTracking()
            .Include(a => a.Department)
            .Include(a => a.Groups.OrderBy(g => g.Sequence))
                .ThenInclude(g => g.Expenses.OrderBy(e => e.Sequence))
                    .ThenInclude(e => e.BudgetItem)
            .Include(a => a.Expenses.OrderBy(e => e.Sequence))
                .ThenInclude(e => e.BudgetItem)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException("活動不存在");

        var expenseIds = activity.Expenses
            .Concat(activity.Groups.SelectMany(g => g.Expenses))
            .Select(e => e.Id)
            .ToList();

        var remittanceMap = await db.PendingRemittances
            .AsNoTracking()
            .Where(r => r.ActivityExpenseId.HasValue && expenseIds.Contains(r.ActivityExpenseId.Value))
            .ToDictionaryAsync(r => r.ActivityExpenseId!.Value, ct);

        return MapToDetailResponse(activity, remittanceMap);
    }

    public async Task<ActivityDetailResponse> CreateAsync(
        CreateActivityRequest request, CancellationToken ct = default)
    {
        await db.EnsureDepartmentExistsAsync(request.DepartmentId, ct);
        await ValidateBudgetItemsAsync(request.DepartmentId, request.Year, request.Groups, request.Expenses, ct);

        var now = DateTime.UtcNow;
        var nextSequence = await GetNextSequenceAsync(
            request.DepartmentId, request.Year, request.Month, ct);

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            DepartmentId = request.DepartmentId,
            Year = request.Year,
            Month = request.Month,
            Name = request.Name,
            Description = request.Description,
            Sequence = nextSequence,
            CreatedAt = now,
            UpdatedAt = now,
        };

        BuildGroupsAndExpenses(activity, request.Groups, request.Expenses, now);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(ct);

        return await GetDetailAsync(activity.Id, ct);
    }

    public async Task<ActivityDetailResponse> UpdateAsync(
        Guid id, UpdateActivityRequest request, CancellationToken ct = default)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException("活動不存在");
        await ValidateBudgetItemsAsync(activity.DepartmentId, activity.Year, request.Groups, request.Expenses, ct);

        var now = DateTime.UtcNow;

        // 刪除所有現有開銷（不管有無分組）
        var allExpenses = await db.ActivityExpenses
            .Where(e => e.ActivityId == id)
            .ToListAsync(ct);
        if (allExpenses.Count > 0)
        {
            db.ActivityExpenses.RemoveRange(allExpenses);
            await db.SaveChangesAsync(ct);
        }

        // 刪除所有分組
        var allGroups = await db.ActivityGroups
            .Where(g => g.ActivityId == id)
            .ToListAsync(ct);
        if (allGroups.Count > 0)
        {
            db.ActivityGroups.RemoveRange(allGroups);
            await db.SaveChangesAsync(ct);
        }

        // 更新活動基本資訊
        activity.Name = request.Name;
        activity.Description = request.Description;
        activity.UpdatedAt = now;

        // 月份異動：重算目標月份的 Sequence
        if (request.Month.HasValue && request.Month.Value != activity.Month)
        {
            activity.Month = request.Month.Value;
            activity.Sequence = await GetNextSequenceAsync(
                activity.DepartmentId, activity.Year, activity.Month, ct);
        }

        await db.SaveChangesAsync(ct);

        // 建立新的分組/開銷（透過 DbSet 直接新增，避免導航屬性衝突）
        if (request.Groups is not null)
        {
            foreach (var groupInput in request.Groups)
            {
                var group = new ActivityGroup
                {
                    Id = Guid.NewGuid(),
                    ActivityId = id,
                    Name = groupInput.Name,
                    Sequence = groupInput.Sequence,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.ActivityGroups.Add(group);

                foreach (var expenseInput in groupInput.Expenses)
                {
                    db.ActivityExpenses.Add(CreateExpense(id, group.Id, expenseInput, now));
                }
            }
        }

        if (request.Expenses is not null)
        {
            foreach (var expenseInput in request.Expenses)
            {
                db.ActivityExpenses.Add(CreateExpense(id, null, expenseInput, now));
            }
        }

        await db.SaveChangesAsync(ct);

        return await GetDetailAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new KeyNotFoundException("活動不存在");

        db.Activities.Remove(activity);
        await db.SaveChangesAsync(ct);
    }


    private async Task<int> GetNextSequenceAsync(
        Guid departmentId, int year, int month, CancellationToken ct)
    {
        var maxSequence = await db.Activities
            .AsNoTracking()
            .Where(a => a.DepartmentId == departmentId && a.Year == year && a.Month == month)
            .MaxAsync(a => (int?)a.Sequence, ct);

        return (maxSequence ?? 0) + 1;
    }

    private static void BuildGroupsAndExpenses(
        Activity activity,
        List<ActivityGroupInput>? groups,
        List<ActivityExpenseInput>? ungroupedExpenses,
        DateTime now)
    {
        if (groups is not null)
        {
            foreach (var groupInput in groups)
            {
                var group = new ActivityGroup
                {
                    Id = Guid.NewGuid(),
                    ActivityId = activity.Id,
                    Name = groupInput.Name,
                    Sequence = groupInput.Sequence,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                foreach (var expenseInput in groupInput.Expenses)
                {
                    group.Expenses.Add(CreateExpense(activity.Id, group.Id, expenseInput, now));
                }

                activity.Groups.Add(group);
            }
        }

        if (ungroupedExpenses is not null)
        {
            foreach (var expenseInput in ungroupedExpenses)
            {
                activity.Expenses.Add(CreateExpense(activity.Id, null, expenseInput, now));
            }
        }
    }

    private async Task ValidateBudgetItemsAsync(
        Guid departmentId,
        int year,
        List<ActivityGroupInput>? groups,
        List<ActivityExpenseInput>? ungroupedExpenses,
        CancellationToken ct)
    {
        var budgetItemIds = EnumerateExpenseInputs(groups, ungroupedExpenses)
            .Where(input => input.BudgetItemId.HasValue)
            .Select(input => input.BudgetItemId!.Value)
            .Distinct()
            .ToList();
        if (budgetItemIds.Count == 0)
        {
            return;
        }

        var matchedCount = await db.BudgetItems
            .AsNoTracking()
            .Where(item => budgetItemIds.Contains(item.Id))
            .Where(item => item.DepartmentBudget.DepartmentId == departmentId
                && item.DepartmentBudget.AnnualBudget.Year == year)
            .CountAsync(ct);

        if (matchedCount != budgetItemIds.Count)
        {
            throw new ArgumentException("活動細項綁定的預算項目必須屬於相同部門與年度");
        }
    }

    private static IEnumerable<ActivityExpenseInput> EnumerateExpenseInputs(
        List<ActivityGroupInput>? groups,
        List<ActivityExpenseInput>? ungroupedExpenses)
    {
        if (groups is not null)
        {
            foreach (var group in groups)
            {
                foreach (var expense in group.Expenses)
                {
                    yield return expense;
                }
            }
        }

        if (ungroupedExpenses is not null)
        {
            foreach (var expense in ungroupedExpenses)
            {
                yield return expense;
            }
        }
    }

    private static ActivityExpense CreateExpense(
        Guid activityId, Guid? groupId, ActivityExpenseInput input, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            ActivityId = activityId,
            ActivityGroupId = groupId,
            BudgetItemId = input.BudgetItemId,
            Description = input.Description,
            Amount = input.Amount,
            Note = input.Note,
            Sequence = input.Sequence,
            CreatedAt = now,
            UpdatedAt = now,
        };

    private static ActivityDetailResponse MapToDetailResponse(
        Activity activity, Dictionary<Guid, PendingRemittance> remittanceMap)
    {
        var groupedExpenseIds = activity.Groups
            .SelectMany(g => g.Expenses)
            .Select(e => e.Id)
            .ToHashSet();

        var ungroupedExpenses = activity.Expenses
            .Where(e => e.ActivityGroupId is null && !groupedExpenseIds.Contains(e.Id))
            .OrderBy(e => e.Sequence)
            .Select(e => MapExpenseResponse(e, remittanceMap))
            .ToList();

        var groups = activity.Groups
            .OrderBy(g => g.Sequence)
            .Select(g => new ActivityGroupResponse(
                g.Id,
                g.Name,
                g.Sequence,
                g.Expenses.Sum(e => e.Amount),
                g.Expenses.OrderBy(e => e.Sequence).Select(e => MapExpenseResponse(e, remittanceMap)).ToList()))
            .ToList();

        var totalAmount = activity.Expenses.Sum(e => e.Amount);

        return new ActivityDetailResponse(
            activity.Id,
            activity.Name,
            activity.Description,
            activity.DepartmentId,
            activity.Department.Name,
            activity.Year,
            activity.Month,
            activity.Sequence,
            totalAmount,
            groups,
            ungroupedExpenses);
    }

    private static ActivityExpenseResponse MapExpenseResponse(
        ActivityExpense e, Dictionary<Guid, PendingRemittance> remittanceMap)
    {
        remittanceMap.TryGetValue(e.Id, out var remittance);
        return new(
            e.Id,
            e.Description,
            e.Amount,
            e.Note,
            e.Sequence,
            e.BudgetItemId,
            e.BudgetItem?.ActivityName,
            remittance?.Id,
            remittance?.Status);
    }
}
