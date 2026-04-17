using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>
/// 銀行交易 CRUD 與期間查詢。
/// Excel 匯出 → <see cref="IBankTransactionExcelExportService"/>
/// 收據追蹤 → <see cref="IBankTransactionReceiptService"/>
/// </summary>
public class BankTransactionService(ApplicationDbContext db) : IBankTransactionService
{
    public async Task<List<BankTransactionResponse>> GetTransactionsAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);
        var transactions = await GetTransactionsQuery(bankName, startDate, endDate)
            .Include(t => t.Department)
            .Include(t => t.Activity)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);
        var openingBalance = await CalculateOpeningBalanceAsync(bankName, startDate, ct);
        return MapTransactions(transactions, openingBalance);
    }

    public async Task<BankTransactionSummaryResponse> GetPeriodSummaryAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);
        var openingBalance = await CalculateOpeningBalanceAsync(bankName, startDate, ct);
        var periodTransactions = await GetTransactionsQuery(bankName, startDate, endDate).ToListAsync(ct);
        var totalIncome = periodTransactions.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = periodTransactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
            + periodTransactions.Sum(t => t.Fee);

        return new BankTransactionSummaryResponse(
            openingBalance,
            totalIncome,
            totalExpense,
            openingBalance + totalIncome - totalExpense);
    }

    public async Task<BankTransactionResponse> CreateTransactionAsync(
        string bankName, CreateBankTransactionRequest request, CancellationToken ct = default)
    {
        await db.EnsureDepartmentExistsIfProvidedAsync(request.DepartmentId, ct);
        await ValidateActivitySelectionAsync(
            request.TransactionType,
            request.DepartmentId,
            request.ActivityId,
            request.TransactionDate.Year,
            ct);
        var now = DateTime.UtcNow;
        var entity = CreateEntity(bankName, request, now);

        db.BankTransactions.Add(entity);
        await db.SaveChangesAsync(ct);

        return new BankTransactionResponse(
            entity.Id,
            entity.BankName,
            entity.TransactionType,
            entity.TransactionDate,
            entity.Description,
            entity.DepartmentId,
            await GetDepartmentNameAsync(entity.DepartmentId, ct),
            entity.Amount,
            entity.Fee,
            entity.ReceiptCollected,
            entity.ReceiptMailed,
            0,
            entity.ActivityId,
            await GetActivityNameAsync(entity.ActivityId, ct),
            entity.PendingRemittanceId);
    }

    public async Task UpdateTransactionAsync(
        Guid id, UpdateBankTransactionRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankTransactions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        await db.EnsureDepartmentExistsIfProvidedAsync(request.DepartmentId, ct);
        await ValidateActivitySelectionAsync(
            request.TransactionType,
            request.DepartmentId,
            request.ActivityId,
            request.TransactionDate.Year,
            ct);
        ApplyTransactionUpdate(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteTransactionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.BankTransactions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        db.BankTransactions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task BatchUpdateDepartmentAsync(
        BatchUpdateDepartmentRequest request, CancellationToken ct = default)
    {
        await db.EnsureDepartmentExistsIfProvidedAsync(request.DepartmentId, ct);
        var entities = await GetBatchUpdateEntitiesAsync(request, ct);
        var now = DateTime.UtcNow;
        ApplyDepartmentUpdate(entities, request.DepartmentId, now);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<BankTransaction> GetTransactionsQuery(string bankName, DateOnly startDate, DateOnly endDate) =>
        db.BankTransactions
            .AsNoTracking()
            .Where(t => t.BankName == bankName
                        && t.TransactionDate >= startDate
                        && t.TransactionDate <= endDate);

    private static List<BankTransactionResponse> MapTransactions(
        List<BankTransaction> transactions, decimal openingBalance)
    {
        var result = new List<BankTransactionResponse>(transactions.Count);
        var runningBalance = openingBalance;

        foreach (var transaction in transactions)
        {
            runningBalance += GetNetAmount(transaction);
            result.Add(MapTransaction(transaction, runningBalance));
        }

        return result;
    }

    private static BankTransactionResponse MapTransaction(BankTransaction transaction, decimal runningBalance) =>
        new(
            transaction.Id,
            transaction.BankName,
            transaction.TransactionType,
            transaction.TransactionDate,
            transaction.Description,
            transaction.DepartmentId,
            transaction.Department?.Name,
            transaction.Amount,
            transaction.Fee,
            transaction.ReceiptCollected,
            transaction.ReceiptMailed,
            runningBalance,
            transaction.ActivityId,
            transaction.Activity?.Name,
            transaction.PendingRemittanceId);

    private static decimal GetNetAmount(BankTransaction transaction) =>
        (transaction.TransactionType == TransactionType.Income ? transaction.Amount : -transaction.Amount) - transaction.Fee;

    private static BankTransaction CreateEntity(
        string bankName, CreateBankTransactionRequest request, DateTime now)
    {
        var isExpense = request.TransactionType == TransactionType.Expense;
        return new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankName = bankName,
            TransactionType = request.TransactionType,
            TransactionDate = request.TransactionDate,
            Description = request.Description.Trim(),
            DepartmentId = request.DepartmentId,
            Amount = request.Amount,
            Fee = request.Fee,
            ReceiptCollected = isExpense && request.ReceiptCollected,
            ReceiptMailed = isExpense && request.ReceiptMailed,
            ActivityId = request.ActivityId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static void ApplyTransactionUpdate(BankTransaction entity, UpdateBankTransactionRequest request)
    {
        var isExpense = request.TransactionType == TransactionType.Expense;
        entity.TransactionType = request.TransactionType;
        entity.TransactionDate = request.TransactionDate;
        entity.Description = request.Description.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.Amount = request.Amount;
        entity.Fee = request.Fee;
        entity.ReceiptCollected = isExpense && request.ReceiptCollected;
        entity.ReceiptMailed = isExpense && request.ReceiptMailed;
        entity.ActivityId = request.ActivityId;
    }

    private async Task<List<BankTransaction>> GetBatchUpdateEntitiesAsync(
        BatchUpdateDepartmentRequest request, CancellationToken ct)
    {
        var uniqueIds = request.Ids.Distinct().ToList();
        var entities = await db.BankTransactions.Where(t => uniqueIds.Contains(t.Id)).ToListAsync(ct);
        if (entities.Count != uniqueIds.Count)
        {
            throw new KeyNotFoundException("部分交易記錄不存在");
        }

        var (startDate, endDate) = GetPeriodRange(request.Year, request.Month);
        if (entities.Any(t => t.BankName != request.BankName
                              || t.TransactionDate < startDate
                              || t.TransactionDate > endDate))
        {
            throw new ArgumentException("部分交易記錄不在目前查詢範圍內");
        }

        return entities;
    }

    private static void ApplyDepartmentUpdate(
        List<BankTransaction> entities, Guid? departmentId, DateTime now)
    {
        foreach (var entity in entities)
        {
            if (entity.DepartmentId != departmentId)
            {
                entity.ActivityId = null;
            }

            entity.DepartmentId = departmentId;
            entity.UpdatedAt = now;
        }
    }

    private async Task<string?> GetDepartmentNameAsync(Guid? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        return await db.SportsDepartments
            .AsNoTracking()
            .Where(d => d.Id == departmentId.Value)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<string?> GetActivityNameAsync(Guid? activityId, CancellationToken ct)
    {
        if (!activityId.HasValue)
        {
            return null;
        }

        return await db.Activities
            .AsNoTracking()
            .Where(a => a.Id == activityId.Value)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(ct);
    }

    private async Task ValidateActivitySelectionAsync(
        TransactionType transactionType,
        Guid? departmentId,
        Guid? activityId,
        int transactionYear,
        CancellationToken ct)
    {
        if (!activityId.HasValue)
        {
            return;
        }

        if (transactionType != TransactionType.Expense)
        {
            throw new ArgumentException("只有支出交易可以指定來源活動");
        }

        if (!departmentId.HasValue)
        {
            throw new ArgumentException("選擇來源活動時必須同時指定歸屬部門");
        }

        var activity = await db.Activities
            .AsNoTracking()
            .Where(a => a.Id == activityId.Value)
            .Select(a => new
            {
                a.DepartmentId,
                a.Year,
            })
            .FirstOrDefaultAsync(ct);

        if (activity is null)
        {
            throw new ArgumentException("指定的來源活動不存在");
        }

        if (activity.DepartmentId != departmentId.Value)
        {
            throw new ArgumentException("來源活動與歸屬部門不一致");
        }

        if (activity.Year != transactionYear)
        {
            throw new ArgumentException("來源活動年度必須與交易日期年份一致");
        }
    }

    private async Task<decimal> CalculateOpeningBalanceAsync(
        string bankName, DateOnly periodStart, CancellationToken ct)
    {
        var initialBalance = await db.BankInitialBalances
            .AsNoTracking()
            .Where(b => b.BankName == bankName)
            .Select(b => b.InitialAmount)
            .FirstOrDefaultAsync(ct);

        var priorTransactions = await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.BankName == bankName && t.TransactionDate < periodStart)
            .ToListAsync(ct);

        var priorNet = priorTransactions.Sum(t =>
            (t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount) - t.Fee);

        return initialBalance + priorNet;
    }

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
