using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.AnnualBudget;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HansOS.Api.Services;

public interface IBudgetImportService
{
    /// <summary>批量匯入預算項目（覆蓋模式）</summary>
    Task<BudgetImportResultResponse> ImportAsync(
        int year, BudgetImportRequest request, CancellationToken ct = default);

    /// <summary>預覽匯入結果（不寫入資料庫）</summary>
    Task<BudgetImportPreviewResponse> PreviewAsync(
        int year, BudgetImportRequest request, CancellationToken ct = default);
}

public class BudgetImportService(
    ApplicationDbContext db,
    IAnnualBudgetService budgetService,
    ILogger<BudgetImportService> logger) : IBudgetImportService
{
    private const int MinYear = 2020;
    private const int MaxYear = 2100;

    public async Task<BudgetImportResultResponse> ImportAsync(
        int year, BudgetImportRequest request, CancellationToken ct)
    {
        ValidateYear(year);
        ValidateRequest(request);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // 確保年度預算已初始化
        await budgetService.GetOverviewAsync(year, ct);

        var departmentSummaries = new List<DepartmentImportSummary>();

        foreach (var deptInput in request.Departments)
        {
            var (department, isNew) = await FindOrCreateDepartmentAsync(deptInput.DepartmentName.Trim(), ct);

            // 若是新部門，需重新初始化以建立 DepartmentBudget
            if (isNew)
            {
                await budgetService.GetOverviewAsync(year, ct);
            }

            var deptBudget = await db.DepartmentBudgets
                .Include(d => d.AnnualBudget)
                .FirstOrDefaultAsync(d => d.AnnualBudget.Year == year && d.DepartmentId == department.Id, ct)
                ?? throw new KeyNotFoundException($"{year} 年度中找不到部門「{deptInput.DepartmentName}」的預算");

            // 覆蓋模式：刪除現有項目
            var existingItems = await db.BudgetItems
                .Where(i => i.DepartmentBudgetId == deptBudget.Id)
                .ToListAsync(ct);
            db.BudgetItems.RemoveRange(existingItems);

            // 寫入新項目
            var now = DateTime.UtcNow;
            foreach (var item in deptInput.Items)
            {
                db.BudgetItems.Add(new BudgetItem
                {
                    Id = Guid.NewGuid(),
                    DepartmentBudgetId = deptBudget.Id,
                    Sequence = item.Sequence,
                    ActivityName = item.ActivityName,
                    ContentItem = item.ContentItem,
                    Amount = item.Amount,
                    Note = item.Note,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            deptBudget.UpdatedAt = now;

            departmentSummaries.Add(new DepartmentImportSummary(
                deptInput.DepartmentName.Trim(),
                department.Id,
                isNew,
                deptInput.Items.Count,
                deptInput.Items.Sum(i => i.Amount)));
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "預算批量匯入完成: Year={Year}, Departments={Count}, TotalItems={Items}",
            year, departmentSummaries.Count, departmentSummaries.Sum(d => d.ItemCount));

        return new BudgetImportResultResponse(
            year,
            departmentSummaries.Count,
            departmentSummaries.Sum(d => d.ItemCount),
            departmentSummaries.Sum(d => d.TotalAmount),
            departmentSummaries);
    }

    public async Task<BudgetImportPreviewResponse> PreviewAsync(
        int year, BudgetImportRequest request, CancellationToken ct)
    {
        ValidateYear(year);
        ValidateRequest(request);

        var allDepartments = await db.SportsDepartments
            .AsNoTracking()
            .ToListAsync(ct);

        var annualBudget = await db.AnnualBudgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Year == year, ct);

        var departmentPreviews = new List<DepartmentImportPreview>();
        var warnings = new List<string>();

        foreach (var deptInput in request.Departments)
        {
            var trimmedName = deptInput.DepartmentName.Trim();
            var existingDept = allDepartments.FirstOrDefault(d => d.Name == trimmedName);
            var isNew = existingDept is null;

            var existingItemCount = 0;
            if (!isNew && annualBudget is not null)
            {
                existingItemCount = await db.DepartmentBudgets
                    .AsNoTracking()
                    .Where(d => d.AnnualBudgetId == annualBudget.Id && d.DepartmentId == existingDept!.Id)
                    .SelectMany(d => d.Items)
                    .CountAsync(ct);
            }

            if (existingItemCount > 0)
            {
                warnings.Add($"部門「{trimmedName}」現有 {existingItemCount} 筆預算項目將被覆蓋");
            }

            if (isNew)
            {
                warnings.Add($"部門「{trimmedName}」不存在，將自動建立");
            }

            departmentPreviews.Add(new DepartmentImportPreview(
                trimmedName,
                isNew,
                existingItemCount,
                deptInput.Items.Count,
                deptInput.Items.Sum(i => i.Amount),
                deptInput.Items.Select(i => new BudgetItemPreview(
                    i.Sequence, i.ActivityName, i.ContentItem, i.Amount, i.Note)).ToList()));
        }

        return new BudgetImportPreviewResponse(
            year,
            departmentPreviews.Count,
            departmentPreviews.Sum(d => d.NewItemCount),
            departmentPreviews.Sum(d => d.TotalAmount),
            departmentPreviews,
            warnings);
    }

    private async Task<(SportsDepartment Department, bool IsNew)> FindOrCreateDepartmentAsync(
        string name, CancellationToken ct)
    {
        var existing = await db.SportsDepartments.FirstOrDefaultAsync(d => d.Name == name, ct);
        if (existing is not null)
        {
            return (existing, false);
        }

        var now = DateTime.UtcNow;
        var department = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.SportsDepartments.Add(department);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("自動建立新部門: {Name}", name);
        return (department, true);
    }

    private static void ValidateYear(int year)
    {
        if (year < MinYear || year > MaxYear)
        {
            throw new ArgumentException($"年度必須在 {MinYear} 至 {MaxYear} 之間");
        }
    }

    private static void ValidateRequest(BudgetImportRequest request)
    {
        if (request.Departments.Count == 0)
        {
            throw new ArgumentException("至少需要一個部門的預算資料");
        }

        foreach (var dept in request.Departments)
        {
            if (string.IsNullOrWhiteSpace(dept.DepartmentName))
            {
                throw new ArgumentException("部門名稱不可為空");
            }

            if (dept.Items.Count == 0)
            {
                throw new ArgumentException($"部門「{dept.DepartmentName}」至少需要一筆預算項目");
            }
        }
    }
}
