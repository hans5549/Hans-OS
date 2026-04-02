using System.Text.Json;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>個人記帳交易記錄服務</summary>
public class FinanceTransactionService(
    ApplicationDbContext db,
    ILogger<FinanceTransactionService> logger) : IFinanceTransactionService
{
    /// <inheritdoc />
    public async Task<List<DailyTransactionGroup>> GetTransactionsAsync(
        string userId, int year, int month, CancellationToken ct = default)
    {
        ValidateYearMonth(year, month);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var transactions = await db.FinanceTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate
                && t.TransactionDate < endDate)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return transactions
            .GroupBy(t => t.TransactionDate)
            .Select(g => new DailyTransactionGroup(
                g.Key,
                g.Where(t => t.TransactionType == FinanceTransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.TransactionType == FinanceTransactionType.Expense).Sum(t => t.Amount),
                g.Select(MapToResponse).ToList()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MonthlySummaryResponse> GetMonthlySummaryAsync(
        string userId, int year, int month, CancellationToken ct = default)
    {
        ValidateYearMonth(year, month);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var totals = await db.FinanceTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate
                && t.TransactionDate < endDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalIncome = g
                    .Where(t => t.TransactionType == FinanceTransactionType.Income)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                TotalExpense = g
                    .Where(t => t.TransactionType == FinanceTransactionType.Expense
                             || t.TransactionType == FinanceTransactionType.Interest)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .FirstOrDefaultAsync(ct);

        var totalIncome = totals?.TotalIncome ?? 0m;
        var totalExpense = totals?.TotalExpense ?? 0m;

        return new MonthlySummaryResponse(
            year,
            month,
            totalIncome,
            totalExpense,
            totalIncome - totalExpense);
    }

    /// <inheritdoc />
    public async Task<TrendResponse> GetTrendAsync(
        string userId, int startYear, int startMonth,
        int endYear, int endMonth, CancellationToken ct = default)
    {
        ValidateYearMonth(startYear, startMonth);
        ValidateYearMonth(endYear, endMonth);

        var startDate = new DateOnly(startYear, startMonth, 1);
        var endDate = new DateOnly(endYear, endMonth, 1).AddMonths(1);

        if (startDate >= endDate)
        {
            throw new ArgumentException("起始月份必須早於結束月份");
        }

        var monthCount = ((endYear - startYear) * 12) + (endMonth - startMonth) + 1;
        if (monthCount > 12)
        {
            throw new ArgumentException("趨勢查詢最多 12 個月");
        }

        var monthlyData = await db.FinanceTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate
                && t.TransactionDate < endDate)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalIncome = g
                    .Where(t => t.TransactionType == FinanceTransactionType.Income)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                TotalExpense = g
                    .Where(t => t.TransactionType == FinanceTransactionType.Expense
                             || t.TransactionType == FinanceTransactionType.Interest)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        var months = new List<MonthlyTrendPoint>();
        var current = startDate;

        while (current < endDate)
        {
            var data = monthlyData
                .FirstOrDefault(d => d.Year == current.Year && d.Month == current.Month);

            var income = data?.TotalIncome ?? 0m;
            var expense = data?.TotalExpense ?? 0m;

            months.Add(new MonthlyTrendPoint(
                current.Year,
                current.Month,
                income,
                expense,
                income - expense));

            current = current.AddMonths(1);
        }

        return new TrendResponse(months);
    }

    /// <inheritdoc />
    public async Task<CategoryBreakdownResponse> GetCategoryBreakdownAsync(
        string userId, int year, int month, string type, CancellationToken ct = default)
    {
        ValidateYearMonth(year, month);

        if (!Enum.TryParse<FinanceTransactionType>(type, ignoreCase: true, out var transactionType)
            || (transactionType != FinanceTransactionType.Expense
                && transactionType != FinanceTransactionType.Income))
        {
            throw new ArgumentException("類型必須為 Expense 或 Income");
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var matchingTypes = transactionType == FinanceTransactionType.Expense
            ? new[] { FinanceTransactionType.Expense, FinanceTransactionType.Interest }
            : new[] { FinanceTransactionType.Income };

        var breakdown = await db.FinanceTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate
                && t.TransactionDate < endDate
                && matchingTypes.Contains(t.TransactionType)
                && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Category.Icon })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId!.Value,
                g.Key.Name,
                g.Key.Icon,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count(),
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);

        var total = breakdown.Sum(x => x.Amount);

        var items = breakdown.Select(x => new CategoryBreakdownItem(
            x.CategoryId,
            x.Name,
            x.Icon,
            x.Amount,
            total == 0 ? 0 : Math.Round(x.Amount / total * 100, 1),
            x.Count)).ToList();

        return new CategoryBreakdownResponse(year, month, type, total, items);
    }

    /// <inheritdoc />
    public async Task<TransactionResponse> CreateTransactionAsync(
        string userId, CreateTransactionRequest request, CancellationToken ct = default)
    {
        var transactionType = ParseTransactionType(request.TransactionType);
        ValidateTransactionRequest(transactionType, request.CategoryId, request.AccountId, request.ToAccountId);

        var now = DateTime.UtcNow;
        var transaction = new FinanceTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TransactionType = transactionType,
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Note = request.Note,
            Currency = request.Currency,
            Project = request.Project,
            Tags = request.Tags,
            CategoryId = request.CategoryId,
            AccountId = request.AccountId,
            ToAccountId = request.ToAccountId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.FinanceTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "交易已建立: {TransactionId}, 類型={Type}, 金額={Amount}",
            transaction.Id, transactionType, request.Amount);

        return await LoadTransactionResponseAsync(transaction.Id, ct);
    }

    /// <inheritdoc />
    public async Task<TransactionResponse> UpdateTransactionAsync(
        string userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        var transactionType = ParseTransactionType(request.TransactionType);
        ValidateTransactionRequest(transactionType, request.CategoryId, request.AccountId, request.ToAccountId);

        var transaction = await db.FinanceTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        transaction.TransactionType = transactionType;
        transaction.Amount = request.Amount;
        transaction.TransactionDate = request.TransactionDate;
        transaction.Note = request.Note;
        transaction.Currency = request.Currency;
        transaction.Project = request.Project;
        transaction.Tags = request.Tags;
        transaction.CategoryId = request.CategoryId;
        transaction.AccountId = request.AccountId;
        transaction.ToAccountId = request.ToAccountId;
        transaction.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("交易已更新: {TransactionId}", transactionId);

        return await LoadTransactionResponseAsync(transaction.Id, ct);
    }

    /// <inheritdoc />
    public async Task DeleteTransactionAsync(
        string userId, Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await db.FinanceTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        db.FinanceTransactions.Remove(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("交易已刪除: {TransactionId}", transactionId);
    }

    private static FinanceTransactionType ParseTransactionType(string type)
    {
        if (!Enum.TryParse<FinanceTransactionType>(type, ignoreCase: true, out var result))
        {
            throw new ArgumentException("無效的交易類型");
        }

        return result;
    }

    private static void ValidateTransactionRequest(
        FinanceTransactionType type, Guid? categoryId, Guid accountId, Guid? toAccountId)
    {
        switch (type)
        {
            case FinanceTransactionType.Transfer:
                if (toAccountId is null)
                {
                    throw new ArgumentException("轉帳交易必須指定目標帳戶");
                }

                if (accountId == toAccountId)
                {
                    throw new ArgumentException("來源帳戶與目標帳戶不可相同");
                }

                break;

            case FinanceTransactionType.BalanceAdjustment:
                break;

            case FinanceTransactionType.Expense:
            case FinanceTransactionType.Income:
            case FinanceTransactionType.Interest:
                if (categoryId is null)
                {
                    throw new ArgumentException("此交易類型必須指定分類");
                }

                break;
        }
    }

    private static void ValidateYearMonth(int year, int month)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ArgumentException("年度必須在 2000 至 2100 之間");
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentException("月份必須在 1 至 12 之間");
        }
    }

    /// <summary>載入交易記錄含關聯資料並轉為回應 DTO</summary>
    private async Task<TransactionResponse> LoadTransactionResponseAsync(
        Guid transactionId, CancellationToken ct)
    {
        var t = await db.FinanceTransactions
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Account)
            .Include(x => x.ToAccount)
            .FirstAsync(x => x.Id == transactionId, ct);

        return MapToResponse(t);
    }

    private static TransactionResponse MapToResponse(FinanceTransaction t)
    {
        List<string>? tags = null;
        if (!string.IsNullOrEmpty(t.Tags))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(t.Tags);
            }
            catch (JsonException)
            {
                // 忽略無效 JSON
            }
        }

        return new TransactionResponse(
            t.Id,
            t.TransactionType.ToString(),
            t.Amount,
            t.TransactionDate,
            t.Note,
            t.Currency,
            t.Project,
            tags,
            t.CategoryId,
            t.Category?.Name,
            t.Category?.Icon,
            t.AccountId,
            t.Account.Name,
            t.ToAccountId,
            t.ToAccount?.Name);
    }
}
