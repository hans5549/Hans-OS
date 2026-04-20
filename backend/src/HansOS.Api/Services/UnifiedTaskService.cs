using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.FinanceTasks;

using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class UnifiedTaskService(ApplicationDbContext db) : IUnifiedTaskService
{
    public async Task<UnifiedTaskListResponse> GetUnifiedTasksAsync(
        int? year = null,
        int? month = null,
        FinanceTaskStatus? status = null,
        UnifiedTaskType? type = null,
        CancellationToken ct = default)
    {
        var tasks = new List<UnifiedTaskItem>();

        if (type is null or UnifiedTaskType.General)
        {
            tasks.AddRange(await GetGeneralTasksAsync(year, month, status, ct));
        }

        if (type is null or UnifiedTaskType.Remittance)
        {
            tasks.AddRange(await GetRemittanceTasksAsync(year, month, status, ct));
        }

        if (type is null or UnifiedTaskType.Receipt)
        {
            tasks.AddRange(await GetReceiptTasksAsync(year, month, status, ct));
        }

        // 排序：優先度高先、建立時間新先
        tasks = tasks
            .OrderBy(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToList();

        return new UnifiedTaskListResponse(
            tasks,
            tasks.Count,
            tasks.Count(t => t.Status == FinanceTaskStatus.Pending),
            tasks.Count(t => t.Status == FinanceTaskStatus.InProgress),
            tasks.Count(t => t.Status == FinanceTaskStatus.Completed));
    }

    private async Task<List<UnifiedTaskItem>> GetGeneralTasksAsync(
        int? year, int? month, FinanceTaskStatus? status, CancellationToken ct)
    {
        var query = db.FinanceTasks
            .AsNoTracking()
            .Include(t => t.Department)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(t => t.CreatedAt.Month == month.Value);
        }

        return await query
            .Select(t => new UnifiedTaskItem(
                t.Id,
                t.Title,
                t.Description,
                UnifiedTaskType.General,
                t.Priority,
                t.Status,
                t.DueDate,
                t.Department != null ? t.Department.Name : null,
                t.CreatedAt,
                null))
            .ToListAsync(ct);
    }

    private async Task<List<UnifiedTaskItem>> GetRemittanceTasksAsync(
        int? year, int? month, FinanceTaskStatus? status, CancellationToken ct)
    {
        var query = db.PendingRemittances
            .AsNoTracking()
            .Include(r => r.Department)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(r => r.CreatedAt.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(r => r.CreatedAt.Month == month.Value);
        }

        // 映射 PendingRemittanceStatus → FinanceTaskStatus
        if (status.HasValue)
        {
            query = status.Value switch
            {
                FinanceTaskStatus.Pending =>
                    query.Where(r => r.Status == PendingRemittanceStatus.Pending),
                FinanceTaskStatus.Completed =>
                    query.Where(r => r.Status == PendingRemittanceStatus.Completed),
                // InProgress 對待匯款不適用，回空
                _ => query.Where(_ => false),
            };
        }

        return await query
            .Select(r => new UnifiedTaskItem(
                r.Id,
                $"匯款：{r.Description}",
                $"{r.SourceAccount} → {r.TargetAccount}，金額 {r.Amount:N0}",
                UnifiedTaskType.Remittance,
                FinanceTaskPriority.High,
                r.Status == PendingRemittanceStatus.Completed
                    ? FinanceTaskStatus.Completed
                    : FinanceTaskStatus.Pending,
                r.ExpectedDate,
                r.Department != null ? r.Department.Name : null,
                r.CreatedAt,
                r.Id))
            .ToListAsync(ct);
    }

    private async Task<List<UnifiedTaskItem>> GetReceiptTasksAsync(
        int? year, int? month, FinanceTaskStatus? status, CancellationToken ct)
    {
        var query = db.BankTransactions
            .AsNoTracking()
            .Include(t => t.Department)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(t => t.TransactionDate.Month == month.Value);
        }

        // 收據任務狀態：已收+已寄=Completed，已收未寄=InProgress，未收=Pending
        if (status.HasValue)
        {
            query = status.Value switch
            {
                FinanceTaskStatus.Pending =>
                    query.Where(t => !t.ReceiptCollected),
                FinanceTaskStatus.InProgress =>
                    query.Where(t => t.ReceiptCollected && !t.ReceiptMailed),
                FinanceTaskStatus.Completed =>
                    query.Where(t => t.ReceiptCollected && t.ReceiptMailed),
                _ => query,
            };
        }

        return await query
            .Select(t => new UnifiedTaskItem(
                t.Id,
                $"收據：{t.Description}",
                $"{t.BankName} {t.TransactionDate:yyyy/MM/dd}，金額 {t.Amount:N0}",
                UnifiedTaskType.Receipt,
                FinanceTaskPriority.Medium,
                t.ReceiptCollected && t.ReceiptMailed
                    ? FinanceTaskStatus.Completed
                    : t.ReceiptCollected
                        ? FinanceTaskStatus.InProgress
                        : FinanceTaskStatus.Pending,
                null,
                t.Department != null ? t.Department.Name : null,
                t.CreatedAt,
                t.Id))
            .ToListAsync(ct);
    }
}
