using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>個人記帳分析查詢實作（月度/趨勢/分類佔比）</summary>
public class FinanceTransactionAnalyticsService(ApplicationDbContext db) : IFinanceTransactionAnalyticsService
{
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
}
