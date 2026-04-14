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
        await ValidateDepartmentExistsAsync(request.DepartmentId, ct);
        await ValidateActivityAsync(request.TransactionType, request.DepartmentId, request.ActivityId, ct);
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
            null,
            entity.PendingRemittanceId);
    }

    public async Task UpdateTransactionAsync(
        Guid id, UpdateBankTransactionRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankTransactions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        await ValidateDepartmentExistsAsync(request.DepartmentId, ct);
        await ValidateActivityAsync(request.TransactionType, request.DepartmentId, request.ActivityId, ct);
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

    public async Task<byte[]> ExportToExcelAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var transactions = await GetTransactionsAsync(bankName, year, month, ct);
        var summary = await GetPeriodSummaryAsync(bankName, year, month, ct);
        var periodLabel = GetPeriodLabel(year, month);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{bankName}收支表");
        var headers = GetExportHeaders();

        ConfigureTitle(worksheet, bankName, periodLabel, headers.Length);
        ConfigureSummaryRow(worksheet, summary);
        ConfigureHeaderRow(worksheet, headers);
        PopulateDataRows(worksheet, transactions);
        ConfigureTotalsRow(worksheet, transactions, summary, headers.Length);
        FormatCurrencyColumns(worksheet);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ReceiptTrackingSummaryResponse> GetReceiptTrackingAsync(
        int year, int? month = null, CancellationToken ct = default)
    {
        var (startDate, endDate) = GetPeriodRange(year, month);
        var items = await GetReceiptTrackingItemsAsync(startDate, endDate, ct);
        return CreateReceiptTrackingSummary(items);
    }

    public async Task BatchUpdateDepartmentAsync(
        BatchUpdateDepartmentRequest request, CancellationToken ct = default)
    {
        await ValidateDepartmentExistsAsync(request.DepartmentId, ct);
        var entities = await GetBatchUpdateEntitiesAsync(request.Ids, ct);
        await ValidateBatchDepartmentUpdateAsync(entities, request.DepartmentId, ct);
        var now = DateTime.UtcNow;
        ApplyDepartmentUpdate(entities, request.DepartmentId, now);
        await db.SaveChangesAsync(ct);
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

    private static ReceiptTrackingSummaryResponse CreateReceiptTrackingSummary(List<ReceiptTrackingResponse> items) =>
        new(
            items.Count,
            items.Count(i => !i.ReceiptCollected),
            items.Count(i => !i.ReceiptMailed),
            items);

    private async Task ValidateDepartmentExistsAsync(Guid? departmentId, CancellationToken ct)
    {
        if (!departmentId.HasValue)
        {
            return;
        }

        var departmentExists = await db.SportsDepartments.AnyAsync(d => d.Id == departmentId.Value, ct);
        if (!departmentExists)
        {
            throw new ArgumentException("指定的部門不存在");
        }
    }

    private async Task ValidateActivityAsync(
        TransactionType transactionType,
        Guid? departmentId,
        Guid? activityId,
        CancellationToken ct)
    {
        if (!activityId.HasValue)
        {
            return;
        }

        if (transactionType != TransactionType.Expense)
        {
            throw new ArgumentException("只有支出交易可以關聯活動");
        }

        if (!departmentId.HasValue)
        {
            throw new ArgumentException("關聯活動時必須指定部門");
        }

        var activityDepartmentId = await db.Activities
            .AsNoTracking()
            .Where(a => a.Id == activityId.Value)
            .Select(a => (Guid?)a.DepartmentId)
            .FirstOrDefaultAsync(ct);

        if (activityDepartmentId is null)
        {
            throw new ArgumentException("指定的活動不存在");
        }

        if (activityDepartmentId != departmentId.Value)
        {
            throw new ArgumentException("指定的活動不屬於該部門");
        }
    }

    private async Task ValidateBatchDepartmentUpdateAsync(
        List<BankTransaction> entities,
        Guid? departmentId,
        CancellationToken ct)
    {
        var activityIds = entities
            .Where(entity => entity.ActivityId.HasValue)
            .Select(entity => entity.ActivityId!.Value)
            .Distinct()
            .ToList();
        if (activityIds.Count == 0)
        {
            return;
        }

        if (!departmentId.HasValue)
        {
            throw new ArgumentException("已關聯活動的交易不可清除部門");
        }

        var activityDepartmentIds = await db.Activities
            .AsNoTracking()
            .Where(activity => activityIds.Contains(activity.Id))
            .Select(activity => activity.DepartmentId)
            .Distinct()
            .ToListAsync(ct);

        if (activityDepartmentIds.Count != 1 || activityDepartmentIds[0] != departmentId.Value)
        {
            throw new ArgumentException("已關聯活動的交易不可改到其他部門");
        }
    }

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
        List<Guid> ids, CancellationToken ct)
    {
        var uniqueIds = ids.Distinct().ToList();
        var entities = await db.BankTransactions.Where(t => uniqueIds.Contains(t.Id)).ToListAsync(ct);
        if (entities.Count != uniqueIds.Count)
        {
            throw new KeyNotFoundException("部分交易記錄不存在");
        }

        return entities;
    }

    private static void ApplyDepartmentUpdate(
        List<BankTransaction> entities, Guid? departmentId, DateTime now)
    {
        foreach (var entity in entities)
        {
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

    private static string[] GetExportHeaders() =>
        ["日期", "摘要", "歸屬部門", "收入", "支出", "手續費", "餘額", "已回收", "已寄送"];

    private static void ConfigureTitle(IXLWorksheet worksheet, string bankName, string periodLabel, int columnCount)
    {
        worksheet.Cell(1, 1).Value = $"{bankName}收支表 — {periodLabel}";
        worksheet.Range(1, 1, 1, columnCount).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void ConfigureSummaryRow(IXLWorksheet worksheet, BankTransactionSummaryResponse summary)
    {
        worksheet.Cell(3, 1).Value = "期初餘額";
        worksheet.Cell(3, 2).Value = summary.OpeningBalance;
        worksheet.Cell(3, 3).Value = "期間收入";
        worksheet.Cell(3, 4).Value = summary.TotalIncome;
        worksheet.Cell(3, 5).Value = "期間支出";
        worksheet.Cell(3, 6).Value = summary.TotalExpense;
        worksheet.Cell(3, 7).Value = "期末餘額";
        worksheet.Cell(3, 8).Value = summary.ClosingBalance;
    }

    private static void ConfigureHeaderRow(IXLWorksheet worksheet, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(5, i + 1).Value = headers[i];
        }

        worksheet.Range(5, 1, 5, headers.Length).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.LightGray);
    }

    private static void PopulateDataRows(IXLWorksheet worksheet, List<BankTransactionResponse> transactions)
    {
        for (var row = 0; row < transactions.Count; row++)
        {
            var transaction = transactions[row];
            var worksheetRow = row + 6;

            worksheet.Cell(worksheetRow, 1).Value = transaction.TransactionDate.ToString("yyyy/MM/dd");
            worksheet.Cell(worksheetRow, 2).Value = transaction.Description;
            worksheet.Cell(worksheetRow, 3).Value = transaction.DepartmentName ?? string.Empty;
            worksheet.Cell(worksheetRow, 4).Value = transaction.TransactionType == TransactionType.Income ? transaction.Amount : 0;
            worksheet.Cell(worksheetRow, 5).Value = transaction.TransactionType == TransactionType.Expense ? transaction.Amount : 0;
            worksheet.Cell(worksheetRow, 6).Value = transaction.Fee;
            worksheet.Cell(worksheetRow, 7).Value = transaction.RunningBalance;
            worksheet.Cell(worksheetRow, 8).Value = GetReceiptMarker(transaction.TransactionType, transaction.ReceiptCollected);
            worksheet.Cell(worksheetRow, 9).Value = GetReceiptMarker(transaction.TransactionType, transaction.ReceiptMailed);
        }
    }

    private static string GetReceiptMarker(TransactionType transactionType, bool value) =>
        transactionType == TransactionType.Expense ? (value ? "✓" : "✗") : string.Empty;

    private static void ConfigureTotalsRow(
        IXLWorksheet worksheet,
        List<BankTransactionResponse> transactions,
        BankTransactionSummaryResponse summary,
        int columnCount)
    {
        var totalFees = transactions.Sum(t => t.Fee);
        var totalRow = transactions.Count + 6;

        worksheet.Cell(totalRow, 1).Value = "合計";
        worksheet.Cell(totalRow, 4).Value = summary.TotalIncome;
        worksheet.Cell(totalRow, 5).Value = summary.TotalExpense - totalFees;
        worksheet.Cell(totalRow, 6).Value = totalFees;
        worksheet.Range(totalRow, 1, totalRow, columnCount).Style.Font.SetBold(true);
    }

    private static void FormatCurrencyColumns(IXLWorksheet worksheet)
    {
        foreach (var column in new[] { 4, 5, 6, 7 })
        {
            worksheet.Column(column).Style.NumberFormat.Format = "#,##0";
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

        var priorNet = priorTransactions.Sum(GetNetAmount);

        return initialBalance + priorNet;
    }

    private static string GetPeriodLabel(int year, int? month) =>
        month.HasValue ? $"{year}年{month}月" : $"{year}年度";

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
