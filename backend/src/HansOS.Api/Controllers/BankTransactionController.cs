using HansOS.Api.Common;
using HansOS.Api.Models.BankTransactions;
using HansOS.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("bank-transactions")]
[Authorize]
public class BankTransactionController(
    IBankTransactionService transactionService,
    IBankTransactionImportService importService,
    IBankTransactionExcelExportService excelExportService,
    IBankTransactionReceiptService receiptService) : ControllerBase
{
    [HttpGet("{bankName}")]
    public async Task<ApiEnvelope<List<BankTransactionResponse>>> GetTransactions(
        string bankName, [FromQuery] int year, [FromQuery] int? month, CancellationToken ct) =>
        ApiEnvelope<List<BankTransactionResponse>>.Success(
            await transactionService.GetTransactionsAsync(bankName, year, month, ct));

    [HttpGet("{bankName}/summary")]
    public async Task<ApiEnvelope<BankTransactionSummaryResponse>> GetSummary(
        string bankName, [FromQuery] int year, [FromQuery] int? month, CancellationToken ct) =>
        ApiEnvelope<BankTransactionSummaryResponse>.Success(
            await transactionService.GetPeriodSummaryAsync(bankName, year, month, ct));

    [HttpPost("{bankName}")]
    public async Task<ApiEnvelope<BankTransactionResponse>> CreateTransaction(
        string bankName, [FromBody] CreateBankTransactionRequest request, CancellationToken ct) =>
        ApiEnvelope<BankTransactionResponse>.Success(
            await transactionService.CreateTransactionAsync(bankName, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<object?>> UpdateTransaction(
        Guid id, [FromBody] UpdateBankTransactionRequest request, CancellationToken ct)
    {
        await transactionService.UpdateTransactionAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpPost("batch-department")]
    public async Task<ApiEnvelope<object?>> BatchUpdateDepartment(
        [FromBody] BatchUpdateDepartmentRequest request, CancellationToken ct)
    {
        await transactionService.BatchUpdateDepartmentAsync(request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteTransaction(Guid id, CancellationToken ct)
    {
        await transactionService.DeleteTransactionAsync(id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpGet("{bankName}/export")]
    public async Task<IActionResult> ExportToExcel(
        string bankName, [FromQuery] int year, [FromQuery] int? month, CancellationToken ct)
    {
        var bytes = await excelExportService.ExportAsync(bankName, year, month, ct);
        var periodLabel = GetPeriodLabel(year, month);
        var fileName = $"{bankName}收支表_{periodLabel}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("receipt-tracking")]
    public async Task<ApiEnvelope<ReceiptTrackingSummaryResponse>> GetReceiptTracking(
        [FromQuery] int year, [FromQuery] int? month, CancellationToken ct) =>
        ApiEnvelope<ReceiptTrackingSummaryResponse>.Success(
            await receiptService.GetReceiptTrackingAsync(year, month, ct));

    [HttpPatch("{id:guid}/receipt-status")]
    public async Task<ApiEnvelope<object?>> PatchReceiptStatus(
        Guid id, [FromBody] PatchReceiptStatusRequest request, CancellationToken ct)
    {
        await receiptService.PatchReceiptStatusAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpPost("import")]
    public async Task<ApiEnvelope<ImportResultResponse>> ImportHistoricalData(CancellationToken ct)
    {
        try
        {
            var result = await importService.ImportFromSeedDataAsync(ct);
            return ApiEnvelope<ImportResultResponse>.Success(result);
        }
        catch (InvalidOperationException ex)
        {
            return ApiEnvelope<ImportResultResponse>.Fail(ex.Message);
        }
    }

    private static string GetPeriodLabel(int year, int? month) =>
        month.HasValue ? $"{year}年{month}月" : $"{year}年度";
}
