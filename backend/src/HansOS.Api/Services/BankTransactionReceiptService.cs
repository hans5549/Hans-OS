using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>
/// 銀行交易「收據追蹤」相關服務：回收/寄送狀態查詢與更新。
/// </summary>
public class BankTransactionReceiptService(ApplicationDbContext db) : IBankTransactionReceiptService
{
    public async Task<ReceiptTrackingSummaryResponse> GetReceiptTrackingAsync(
        int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);
        var items = await GetReceiptTrackingItemsAsync(startDate, endDate, ct);
        return CreateReceiptTrackingSummary(items);
    }

    public async Task PatchReceiptStatusAsync(
        Guid id, PatchReceiptStatusRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankTransactions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        if (request.ReceiptCollected.HasValue)
        {
            entity.ReceiptCollected = request.ReceiptCollected.Value;
        }

        if (request.ReceiptMailed.HasValue)
        {
            entity.ReceiptMailed = request.ReceiptMailed.Value;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<ReceiptTrackingResponse>> GetReceiptTrackingItemsAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct) =>
        await db.BankTransactions
            .AsNoTracking()
            .Include(t => t.Department)
            .Where(t => t.TransactionDate >= startDate
                        && t.TransactionDate <= endDate
                        && t.TransactionType == TransactionType.Expense
                        && (!t.ReceiptCollected || !t.ReceiptMailed))
            .OrderBy(t => t.BankName)
            .ThenBy(t => t.TransactionDate)
            .Select(t => new ReceiptTrackingResponse(
                t.Id,
                t.BankName,
                t.TransactionDate,
                t.Description,
                t.DepartmentId,
                t.Department != null ? t.Department.Name : null,
                t.Amount,
                t.ReceiptCollected,
                t.ReceiptMailed))
            .ToListAsync(ct);

    private static ReceiptTrackingSummaryResponse CreateReceiptTrackingSummary(
        List<ReceiptTrackingResponse> items) =>
        new(
            items.Count,
            items.Count(i => !i.ReceiptCollected),
            items.Count(i => !i.ReceiptMailed),
            items);

    private static (DateOnly Start, DateOnly End) GetPeriodRange(int year, int? month)
    {
        if (month.HasValue)
        {
            var start = new DateOnly(year, month.Value, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        return (new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
    }
}
