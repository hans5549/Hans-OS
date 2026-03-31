using ClosedXML.Excel;

using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;

using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class BankTransactionService(ApplicationDbContext db) : IBankTransactionService
{
    public async Task<List<BankTransactionResponse>> GetTransactionsAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);

        var transactions = await db.BankTransactions
            .AsNoTracking()
            .Include(t => t.Department)
            .Where(t => t.BankName == bankName
                        && t.TransactionDate >= startDate
                        && t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var openingBalance = await CalculateOpeningBalanceAsync(bankName, startDate, ct);

        var result = new List<BankTransactionResponse>(transactions.Count);
        var runningBalance = openingBalance;

        foreach (var t in transactions)
        {
            runningBalance += t.TransactionType == TransactionType.Income ? t.Amount : -t.Amount;
            runningBalance -= t.Fee;

            result.Add(new BankTransactionResponse(
                t.Id,
                t.BankName,
                t.TransactionType,
                t.TransactionDate,
                t.Description,
                t.DepartmentId,
                t.Department?.Name,
                t.RequestingUnit,
                t.Amount,
                t.Fee,
                t.HasReceipt,
                t.ReceiptCollected,
                t.ReceiptMailed,
                runningBalance));
        }

        return result;
    }

    public async Task<BankTransactionSummaryResponse> GetPeriodSummaryAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);

        var openingBalance = await CalculateOpeningBalanceAsync(bankName, startDate, ct);

        var periodTransactions = await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.BankName == bankName
                        && t.TransactionDate >= startDate
                        && t.TransactionDate <= endDate)
            .ToListAsync(ct);

        var totalIncome = periodTransactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = periodTransactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount)
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
        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.SportsDepartments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!departmentExists)
            {
                throw new ArgumentException("指定的部門不存在");
            }
        }

        var now = DateTime.UtcNow;
        var entity = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankName = bankName,
            TransactionType = request.TransactionType,
            TransactionDate = request.TransactionDate,
            Description = request.Description.Trim(),
            DepartmentId = request.DepartmentId,
            RequestingUnit = request.RequestingUnit?.Trim(),
            Amount = request.Amount,
            Fee = request.Fee,
            HasReceipt = request.HasReceipt,
            ReceiptCollected = request.ReceiptCollected,
            ReceiptMailed = request.ReceiptMailed,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.BankTransactions.Add(entity);
        await db.SaveChangesAsync(ct);

        var department = entity.DepartmentId.HasValue
            ? await db.SportsDepartments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == entity.DepartmentId, ct)
            : null;

        return new BankTransactionResponse(
            entity.Id,
            entity.BankName,
            entity.TransactionType,
            entity.TransactionDate,
            entity.Description,
            entity.DepartmentId,
            department?.Name,
            entity.RequestingUnit,
            entity.Amount,
            entity.Fee,
            entity.HasReceipt,
            entity.ReceiptCollected,
            entity.ReceiptMailed,
            0);
    }

    public async Task UpdateTransactionAsync(
        Guid id, UpdateBankTransactionRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankTransactions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.SportsDepartments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!departmentExists)
            {
                throw new ArgumentException("指定的部門不存在");
            }
        }

        entity.TransactionType = request.TransactionType;
        entity.TransactionDate = request.TransactionDate;
        entity.Description = request.Description.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.RequestingUnit = request.RequestingUnit?.Trim();
        entity.Amount = request.Amount;
        entity.Fee = request.Fee;
        entity.HasReceipt = request.HasReceipt;
        entity.ReceiptCollected = request.ReceiptCollected;
        entity.ReceiptMailed = request.ReceiptMailed;
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

    public async Task<byte[]> ExportToExcelAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var transactions = await GetTransactionsAsync(bankName, year, month, ct);
        var summary = await GetPeriodSummaryAsync(bankName, year, month, ct);

        var periodLabel = month.HasValue
            ? $"{year}年{month}月"
            : $"{year}年度";

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{bankName}收支表");

        // Title
        worksheet.Cell(1, 1).Value = $"{bankName}收支表 — {periodLabel}";
        worksheet.Range(1, 1, 1, 11).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Summary row
        worksheet.Cell(3, 1).Value = "期初餘額";
        worksheet.Cell(3, 2).Value = summary.OpeningBalance;
        worksheet.Cell(3, 3).Value = "期間收入";
        worksheet.Cell(3, 4).Value = summary.TotalIncome;
        worksheet.Cell(3, 5).Value = "期間支出";
        worksheet.Cell(3, 6).Value = summary.TotalExpense;
        worksheet.Cell(3, 7).Value = "期末餘額";
        worksheet.Cell(3, 8).Value = summary.ClosingBalance;

        // Headers
        var headers = new[] { "日期", "摘要", "歸屬部門", "需求單位", "收入", "支出", "手續費", "餘額", "收據", "已回收", "已寄送" };
        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(5, i + 1).Value = headers[i];
        }
        worksheet.Range(5, 1, 5, headers.Length).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightGray);

        // Data rows
        for (var row = 0; row < transactions.Count; row++)
        {
            var t = transactions[row];
            var r = row + 6;
            worksheet.Cell(r, 1).Value = t.TransactionDate.ToString("yyyy/MM/dd");
            worksheet.Cell(r, 2).Value = t.Description;
            worksheet.Cell(r, 3).Value = t.DepartmentName ?? "";
            worksheet.Cell(r, 4).Value = t.RequestingUnit ?? "";
            worksheet.Cell(r, 5).Value = t.TransactionType == TransactionType.Income ? t.Amount : 0;
            worksheet.Cell(r, 6).Value = t.TransactionType == TransactionType.Expense ? t.Amount : 0;
            worksheet.Cell(r, 7).Value = t.Fee;
            worksheet.Cell(r, 8).Value = t.RunningBalance;
            worksheet.Cell(r, 9).Value = t.HasReceipt ? "✓" : "";
            worksheet.Cell(r, 10).Value = t.ReceiptCollected ? "✓" : "";
            worksheet.Cell(r, 11).Value = t.ReceiptMailed ? "✓" : "";
        }

        // Totals row
        var totalRow = transactions.Count + 6;
        worksheet.Cell(totalRow, 1).Value = "合計";
        worksheet.Cell(totalRow, 5).Value = summary.TotalIncome;
        worksheet.Cell(totalRow, 6).Value = summary.TotalExpense - transactions.Sum(t => t.Fee);
        worksheet.Cell(totalRow, 7).Value = transactions.Sum(t => t.Fee);
        worksheet.Range(totalRow, 1, totalRow, headers.Length).Style.Font.SetBold(true);

        // Format currency columns
        foreach (var col in new[] { 5, 6, 7, 8 })
        {
            worksheet.Column(col).Style.NumberFormat.Format = "#,##0.00";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ReceiptTrackingSummaryResponse> GetReceiptTrackingAsync(
        int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);

        var items = await db.BankTransactions
            .AsNoTracking()
            .Include(t => t.Department)
            .Where(t => t.TransactionDate >= startDate
                        && t.TransactionDate <= endDate
                        && (t.HasReceipt && (!t.ReceiptCollected || !t.ReceiptMailed)))
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
                t.HasReceipt,
                t.ReceiptCollected,
                t.ReceiptMailed))
            .ToListAsync(ct);

        return new ReceiptTrackingSummaryResponse(
            items.Count,
            items.Count(i => !i.ReceiptCollected),
            items.Count(i => !i.ReceiptMailed),
            items);
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
