using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Finance;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>股票投資 API</summary>
[ApiController]
[Route("finance/stocks")]
[Authorize]
public class StockTransactionController(IStockTransactionService stockService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得持股清單</summary>
    [HttpGet("holdings")]
    public async Task<ApiEnvelope<List<StockHoldingResponse>>> GetHoldings(CancellationToken ct)
        => ApiEnvelope<List<StockHoldingResponse>>.Success(
            await stockService.GetHoldingsAsync(CurrentUserId, ct));

    /// <summary>取得交易紀錄</summary>
    [HttpGet("transactions")]
    public async Task<ApiEnvelope<List<StockTransactionResponse>>> GetTransactions(
        [FromQuery] string? symbol, CancellationToken ct)
        => ApiEnvelope<List<StockTransactionResponse>>.Success(
            await stockService.GetTransactionsAsync(CurrentUserId, symbol, ct));

    /// <summary>取得損益摘要</summary>
    [HttpGet("summary")]
    public async Task<ApiEnvelope<StockProfitSummaryResponse>> GetProfitSummary(
        [FromQuery] int? year, CancellationToken ct)
        => ApiEnvelope<StockProfitSummaryResponse>.Success(
            await stockService.GetProfitSummaryAsync(CurrentUserId, year, ct));

    /// <summary>買入股票</summary>
    [HttpPost("buy")]
    public async Task<ApiEnvelope<StockTransactionResponse>> Buy(
        [FromBody] BuyStockRequest request, CancellationToken ct)
        => ApiEnvelope<StockTransactionResponse>.Success(
            await stockService.CreateBuyAsync(CurrentUserId, request, ct));

    /// <summary>賣出股票</summary>
    [HttpPost("sell")]
    public async Task<ApiEnvelope<StockTransactionResponse>> Sell(
        [FromBody] SellStockRequest request, CancellationToken ct)
        => ApiEnvelope<StockTransactionResponse>.Success(
            await stockService.CreateSellAsync(CurrentUserId, request, ct));

    /// <summary>刪除交易紀錄</summary>
    [HttpDelete("transactions/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteTransaction(Guid id, CancellationToken ct)
    {
        await stockService.DeleteTransactionAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
