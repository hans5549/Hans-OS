using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Finance;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>交易記錄 API</summary>
[ApiController]
[Route("finance/transactions")]
[Authorize]
public class FinanceTransactionController(IFinanceTransactionService transactionService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得月度交易記錄（按日期分組）</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<DailyTransactionGroup>>> GetTransactions(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => ApiEnvelope<List<DailyTransactionGroup>>.Success(
            await transactionService.GetTransactionsAsync(CurrentUserId, year, month, ct));

    /// <summary>取得月度摘要</summary>
    [HttpGet("summary")]
    public async Task<ApiEnvelope<MonthlySummaryResponse>> GetMonthlySummary(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => ApiEnvelope<MonthlySummaryResponse>.Success(
            await transactionService.GetMonthlySummaryAsync(CurrentUserId, year, month, ct));

    /// <summary>取得跨月收支趨勢（最多 12 個月）</summary>
    [HttpGet("trends")]
    public async Task<ApiEnvelope<TrendResponse>> GetTrends(
        [FromQuery] int startYear, [FromQuery] int startMonth,
        [FromQuery] int endYear, [FromQuery] int endMonth,
        CancellationToken ct)
        => ApiEnvelope<TrendResponse>.Success(
            await transactionService.GetTrendAsync(CurrentUserId, startYear, startMonth, endYear, endMonth, ct));

    /// <summary>取得月度分類佔比</summary>
    [HttpGet("category-breakdown")]
    public async Task<ApiEnvelope<CategoryBreakdownResponse>> GetCategoryBreakdown(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string type,
        CancellationToken ct)
        => ApiEnvelope<CategoryBreakdownResponse>.Success(
            await transactionService.GetCategoryBreakdownAsync(CurrentUserId, year, month, type, ct));

    /// <summary>新增交易</summary>
    [HttpPost]
    public async Task<ApiEnvelope<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request, CancellationToken ct)
        => ApiEnvelope<TransactionResponse>.Success(
            await transactionService.CreateTransactionAsync(CurrentUserId, request, ct));

    /// <summary>更新交易</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<TransactionResponse>> UpdateTransaction(
        Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
        => ApiEnvelope<TransactionResponse>.Success(
            await transactionService.UpdateTransactionAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除交易</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteTransaction(Guid id, CancellationToken ct)
    {
        await transactionService.DeleteTransactionAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
