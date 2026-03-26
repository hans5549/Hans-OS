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
    IBankTransactionImportService importService) : ControllerBase
{
    [HttpGet("{bankName}")]
    public async Task<ApiEnvelope<List<BankTransactionResponse>>> GetTransactions(
        string bankName, [FromQuery] int year, [FromQuery] int? month, CancellationToken ct)
    {
        var result = await transactionService.GetTransactionsAsync(bankName, year, month, ct);
        return ApiEnvelope<List<BankTransactionResponse>>.Success(result);
    }

    [HttpGet("{bankName}/summary")]
    public async Task<ApiEnvelope<BankTransactionSummaryResponse>> GetSummary(
        string bankName, [FromQuery] int year, [FromQuery] int? month, CancellationToken ct)
    {
        var result = await transactionService.GetPeriodSummaryAsync(bankName, year, month, ct);
        return ApiEnvelope<BankTransactionSummaryResponse>.Success(result);
    }

    [HttpPost("{bankName}")]
    public async Task<ApiEnvelope<BankTransactionResponse>> CreateTransaction(
        string bankName, [FromBody] CreateBankTransactionRequest request, CancellationToken ct)
    {
        var result = await transactionService.CreateTransactionAsync(bankName, request, ct);
        return ApiEnvelope<BankTransactionResponse>.Success(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<object?>> UpdateTransaction(
        Guid id, [FromBody] UpdateBankTransactionRequest request, CancellationToken ct)
    {
        await transactionService.UpdateTransactionAsync(id, request, ct);
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
        var bytes = await transactionService.ExportToExcelAsync(bankName, year, month, ct);
        var periodLabel = month.HasValue ? $"{year}年{month}月" : $"{year}年度";
        var fileName = $"{bankName}收支表_{periodLabel}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("import")]
    public async Task<ApiEnvelope<ImportResultResponse>> ImportHistoricalData(CancellationToken ct)
    {
        var result = await importService.ImportFromSeedDataAsync(ct);
        return ApiEnvelope<ImportResultResponse>.Success(result);
    }
}
