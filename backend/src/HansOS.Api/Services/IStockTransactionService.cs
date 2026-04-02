using HansOS.Api.Models.Finance;

namespace HansOS.Api.Services;

public interface IStockTransactionService
{
    /// <summary>取得目前持股（加權平均成本法）</summary>
    Task<List<StockHoldingResponse>> GetHoldingsAsync(string userId, CancellationToken ct = default);

    /// <summary>取得股票交易記錄，可依股票代號篩選</summary>
    Task<List<StockTransactionResponse>> GetTransactionsAsync(string userId, string? stockSymbol = null, CancellationToken ct = default);

    /// <summary>取得損益摘要，可依年度篩選</summary>
    Task<StockProfitSummaryResponse> GetProfitSummaryAsync(string userId, int? year = null, CancellationToken ct = default);

    /// <summary>建立買入交易</summary>
    Task<StockTransactionResponse> CreateBuyAsync(string userId, BuyStockRequest request, CancellationToken ct = default);

    /// <summary>建立賣出交易</summary>
    Task<StockTransactionResponse> CreateSellAsync(string userId, SellStockRequest request, CancellationToken ct = default);

    /// <summary>刪除交易記錄</summary>
    Task DeleteTransactionAsync(string userId, Guid id, CancellationToken ct = default);
}
