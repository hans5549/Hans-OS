using ClosedXML.Excel;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;

namespace HansOS.Api.Services;

/// <summary>
/// 銀行交易 Excel 匯出服務。
/// 資料來源委派給 <see cref="IBankTransactionService"/>，本服務只負責 XLSX 版面與格式。
/// </summary>
public class BankTransactionExcelExportService(IBankTransactionService transactionService)
    : IBankTransactionExcelExportService
{
    public async Task<byte[]> ExportAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default)
    {
        var transactions = await transactionService.GetTransactionsAsync(bankName, year, month, ct);
        var summary = await transactionService.GetPeriodSummaryAsync(bankName, year, month, ct);
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

    private static string GetPeriodLabel(int year, int? month) =>
        month.HasValue ? $"{year}年{month}月" : $"{year}年度";

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
}
