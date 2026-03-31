using System.Reflection;

using ClosedXML.Excel;

using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;

using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class BankTransactionImportService(
    ApplicationDbContext db,
    ILogger<BankTransactionImportService> logger) : IBankTransactionImportService
{
    private static readonly SemaphoreSlim ImportSemaphore = new(1, 1);

    private static readonly Dictionary<string, string> FileToBank = new()
    {
        { "上海-收支表.xlsx", "上海銀行" },
        { "合作-收支表.xlsx", "合作金庫" },
    };

    private static readonly HashSet<string> SkipSheets = ["憑證說明", "繳交辦法"];

    public async Task<ImportResultResponse> ImportFromSeedDataAsync(CancellationToken ct = default)
    {
        if (!await ImportSemaphore.WaitAsync(0, ct))
        {
            throw new InvalidOperationException("匯入作業正在進行中，請稍後再試");
        }

        try
        {
            return await ExecuteImportAsync(ct);
        }
        finally
        {
            ImportSemaphore.Release();
        }
    }

    private async Task<ImportResultResponse> ExecuteImportAsync(CancellationToken ct)
    {
        var bankDetails = new List<BankImportDetail>();
        var totalTransactions = 0;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var (fileName, bankName) in FileToBank)
            {
                var detail = await ImportFileAsync(fileName, bankName, ct);
                bankDetails.Add(detail);
                totalTransactions += detail.TransactionCount;
            }

            await transaction.CommitAsync(ct);
            logger.LogInformation("歷史資料匯入完成，共匯入 {Total} 筆交易", totalTransactions);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new ImportResultResponse(totalTransactions, bankDetails);
    }

    private async Task<BankImportDetail> ImportFileAsync(
        string fileName, string bankName, CancellationToken ct)
    {
        // 清除該銀行的現有資料
        await db.BankTransactions
            .Where(t => t.BankName == bankName)
            .ExecuteDeleteAsync(ct);
        await db.BankInitialBalances
            .Where(b => b.BankName == bankName)
            .ExecuteDeleteAsync(ct);

        using var stream = GetEmbeddedResource(fileName);
        using var workbook = new XLWorkbook(stream);

        var allTransactions = new List<BankTransaction>();
        decimal? initialBalance = null;

        foreach (var worksheet in workbook.Worksheets)
        {
            if (SkipSheets.Contains(worksheet.Name.Trim()))
            {
                continue;
            }

            var (sheetTransactions, sheetInitialBalance) = ParseSheet(worksheet, bankName);

            if (!initialBalance.HasValue && sheetInitialBalance.HasValue)
            {
                initialBalance = sheetInitialBalance.Value;
            }

            allTransactions.AddRange(sheetTransactions);
        }

        // 建立 BankInitialBalance
        var balanceAmount = initialBalance ?? 0m;
        var now = DateTime.UtcNow;

        db.BankInitialBalances.Add(new BankInitialBalance
        {
            Id = Guid.NewGuid(),
            BankName = bankName,
            InitialAmount = balanceAmount,
            CreatedAt = now,
            UpdatedAt = now,
        });

        db.BankTransactions.AddRange(allTransactions);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "匯入 {BankName}：{Count} 筆交易，初始餘額 {Balance}",
            bankName, allTransactions.Count, balanceAmount);

        return new BankImportDetail(bankName, allTransactions.Count, balanceAmount);
    }

    private (List<BankTransaction> Transactions, decimal? InitialBalance) ParseSheet(
        IXLWorksheet worksheet, string bankName)
    {
        var transactions = new List<BankTransaction>();
        decimal? initialBalance = null;
        BankTransaction? previousTransaction = null;
        var now = DateTime.UtcNow;

        // 資料從 Row 6 開始（Row 1-4 是 Header，Row 5 是欄位名稱，1-based index）
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var row = 6; row <= lastRow; row++)
        {
            var yearCell = worksheet.Cell(row, 2);
            if (yearCell.IsEmpty())
            {
                continue;
            }

            if (!TryGetDecimalValue(yearCell, out var rocYear))
            {
                continue;
            }

            var monthCell = worksheet.Cell(row, 3);
            var dayCell = worksheet.Cell(row, 4);

            if (!TryGetDecimalValue(monthCell, out var month) ||
                !TryGetDecimalValue(dayCell, out var day))
            {
                continue;
            }

            var description = worksheet.Cell(row, 6).GetString().Trim();
            var hasIncome = TryGetDecimalValue(worksheet.Cell(row, 7), out var incomeAmount);
            var hasExpense = TryGetDecimalValue(worksheet.Cell(row, 8), out var expenseAmount);

            var adYear = (int)rocYear + 1911;

            // 年初餘額行：無收支但有餘額
            if (!hasIncome && !hasExpense)
            {
                if (!initialBalance.HasValue && TryGetDecimalValue(worksheet.Cell(row, 9), out var balance))
                {
                    initialBalance = balance;
                }

                continue;
            }

            // 手續費行：合併到上一筆交易的 Fee 欄位
            if (IsFeeRow(description))
            {
                if (previousTransaction is not null)
                {
                    var feeAmount = hasExpense ? expenseAmount : incomeAmount;
                    previousTransaction.Fee = feeAmount;
                }
                else
                {
                    logger.LogWarning("工作表 {Sheet} 第 {Row} 行：手續費無對應的前一筆交易，已跳過",
                        worksheet.Name, row);
                }

                continue;
            }

            // 收支同時有值時記錄警告
            if (hasIncome && hasExpense)
            {
                logger.LogWarning("工作表 {Sheet} 第 {Row} 行：收入與支出欄同時有值，以收入為準",
                    worksheet.Name, row);
            }

            // 一般交易行
            var transactionType = hasIncome ? TransactionType.Income : TransactionType.Expense;
            var amount = hasIncome ? incomeAmount : expenseAmount;

            DateOnly transactionDate;
            try
            {
                transactionDate = new DateOnly(adYear, (int)month, (int)day);
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogWarning("工作表 {Sheet} 第 {Row} 行：日期無效 ({Year}/{Month}/{Day})，已跳過",
                    worksheet.Name, row, adYear, (int)month, (int)day);
                continue;
            }

            var entity = new BankTransaction
            {
                Id = Guid.NewGuid(),
                BankName = bankName,
                TransactionDate = transactionDate,
                Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description,
                TransactionType = transactionType,
                Amount = amount,
                Fee = 0,
                ReceiptCollected = false,
                ReceiptMailed = false,
                CreatedAt = now,
                UpdatedAt = now,
            };

            transactions.Add(entity);
            previousTransaction = entity;
        }

        return (transactions, initialBalance);
    }

    private static bool IsFeeRow(string description)
    {
        return description.Contains("手續費");
    }

    private static bool TryGetDecimalValue(IXLCell cell, out decimal value)
    {
        value = 0;

        if (cell.IsEmpty())
        {
            return false;
        }

        if (cell.DataType == XLDataType.Number)
        {
            value = (decimal)cell.GetDouble();
            return true;
        }

        if (cell.DataType == XLDataType.Text)
        {
            return decimal.TryParse(cell.GetString().Trim(), out value);
        }

        return false;
    }

    private static Stream GetEmbeddedResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName))
            ?? throw new FileNotFoundException($"找不到內嵌資源：{fileName}");

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"無法讀取內嵌資源：{fileName}");
    }
}
