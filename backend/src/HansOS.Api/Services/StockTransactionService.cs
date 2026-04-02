using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>股票交易服務（手續費、證交稅、加權平均成本法）</summary>
public class StockTransactionService(
    ApplicationDbContext db,
    ILogger<StockTransactionService> logger) : IStockTransactionService
{
    /// <summary>券商手續費費率</summary>
    private const decimal CommissionRate = 0.001425m;

    /// <summary>最低手續費（新台幣）</summary>
    private const decimal MinCommission = 20m;

    /// <summary>一般股票證交稅率</summary>
    private const decimal StockTaxRate = 0.003m;

    /// <summary>ETF 證交稅率</summary>
    private const decimal EtfTaxRate = 0.001m;

    /// <inheritdoc />
    public async Task<List<StockHoldingResponse>> GetHoldingsAsync(
        string userId, CancellationToken ct = default)
    {
        var transactions = await db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.TradeDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var holdings = new Dictionary<string, HoldingState>();

        foreach (var t in transactions)
        {
            if (!holdings.TryGetValue(t.StockSymbol, out var state))
            {
                state = new HoldingState { StockName = t.StockName };
                holdings[t.StockSymbol] = state;
            }

            var totalAmount = t.Shares * t.PricePerShare;

            if (t.TradeType == StockTradeType.Buy)
            {
                state.TotalCost += totalAmount + t.Commission;
                state.TotalShares += t.Shares;
            }
            else
            {
                if (state.TotalShares > 0)
                {
                    var averageCost = state.TotalCost / state.TotalShares;
                    var costOfSold = averageCost * t.Shares;
                    state.TotalCost -= costOfSold;
                    state.TotalShares -= t.Shares;
                    state.TotalRealizedPL += (totalAmount - t.Commission - t.Tax) - costOfSold;
                }
            }
        }

        return holdings
            .Where(h => h.Value.TotalShares > 0)
            .Select(h => new StockHoldingResponse(
                h.Key,
                h.Value.StockName,
                h.Value.TotalShares,
                h.Value.TotalShares > 0
                    ? Math.Round(h.Value.TotalCost / h.Value.TotalShares, 4)
                    : 0m,
                Math.Round(h.Value.TotalCost, 2),
                Math.Round(h.Value.TotalRealizedPL, 2)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<StockTransactionResponse>> GetTransactionsAsync(
        string userId, string? stockSymbol = null, CancellationToken ct = default)
    {
        var query = db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (stockSymbol is not null)
        {
            query = query.Where(t => t.StockSymbol == stockSymbol);
        }

        var transactions = await query
            .OrderByDescending(t => t.TradeDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return transactions.Select(MapToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<StockProfitSummaryResponse> GetProfitSummaryAsync(
        string userId, int? year = null, CancellationToken ct = default)
    {
        var query = db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (year.HasValue)
        {
            var startDate = new DateOnly(year.Value, 1, 1);
            var endDate = new DateOnly(year.Value + 1, 1, 1);
            query = query.Where(t => t.TradeDate >= startDate && t.TradeDate < endDate);
        }

        var allTransactions = await query
            .OrderBy(t => t.TradeDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var totalRealizedPL = CalculateTotalRealizedPL(allTransactions);
        var totalCommission = allTransactions.Sum(t => t.Commission);
        var totalTax = allTransactions.Sum(t => t.Tax);

        return new StockProfitSummaryResponse(
            Math.Round(totalRealizedPL, 2),
            Math.Round(totalCommission, 2),
            Math.Round(totalTax, 2),
            allTransactions.Count);
    }

    /// <inheritdoc />
    public async Task<StockTransactionResponse> CreateBuyAsync(
        string userId, BuyStockRequest request, CancellationToken ct = default)
    {
        var totalAmount = request.Shares * request.PricePerShare;
        var commission = CalculateCommission(totalAmount, request.CommissionDiscount);

        var now = DateTime.UtcNow;
        var transaction = new StockTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StockSymbol = request.StockSymbol,
            StockName = request.StockName,
            TradeType = StockTradeType.Buy,
            Shares = request.Shares,
            PricePerShare = request.PricePerShare,
            Commission = commission,
            Tax = 0m,
            TradeDate = request.TradeDate,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.StockTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "股票買入: {Id}, {Symbol} x {Shares} @ {Price}",
            transaction.Id, request.StockSymbol, request.Shares, request.PricePerShare);

        return MapToResponse(transaction);
    }

    /// <inheritdoc />
    public async Task<StockTransactionResponse> CreateSellAsync(
        string userId, SellStockRequest request, CancellationToken ct = default)
    {
        var currentHoldings = await CalculateCurrentHoldingsAsync(
            userId, request.StockSymbol, ct);

        if (request.Shares > currentHoldings)
        {
            throw new ArgumentException(
                $"持有股數不足，目前持有 {currentHoldings} 股，欲賣出 {request.Shares} 股");
        }

        var totalAmount = request.Shares * request.PricePerShare;
        var commission = CalculateCommission(totalAmount, request.CommissionDiscount);
        var tax = CalculateTax(totalAmount, request.IsEtf);

        var stockName = await db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.StockSymbol == request.StockSymbol)
            .Select(t => t.StockName)
            .FirstAsync(ct);

        var now = DateTime.UtcNow;
        var transaction = new StockTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StockSymbol = request.StockSymbol,
            StockName = stockName,
            TradeType = StockTradeType.Sell,
            Shares = request.Shares,
            PricePerShare = request.PricePerShare,
            Commission = commission,
            Tax = tax,
            TradeDate = request.TradeDate,
            Note = request.Note,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.StockTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "股票賣出: {Id}, {Symbol} x {Shares} @ {Price}",
            transaction.Id, request.StockSymbol, request.Shares, request.PricePerShare);

        return await MapToResponseWithPL(transaction, userId, ct);
    }

    /// <inheritdoc />
    public async Task DeleteTransactionAsync(
        string userId, Guid id, CancellationToken ct = default)
    {
        var transaction = await db.StockTransactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("交易記錄不存在");

        db.StockTransactions.Remove(transaction);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("股票交易已刪除: {Id}", id);
    }

    /// <summary>計算手續費（最低 20 元）</summary>
    private static decimal CalculateCommission(decimal totalAmount, decimal commissionDiscount)
    {
        var commission = Math.Floor(totalAmount * CommissionRate * commissionDiscount);
        return Math.Max(commission, MinCommission);
    }

    /// <summary>計算證券交易稅（僅賣出時）</summary>
    private static decimal CalculateTax(decimal totalAmount, bool isEtf)
    {
        var taxRate = isEtf ? EtfTaxRate : StockTaxRate;
        return Math.Floor(totalAmount * taxRate);
    }

    /// <summary>計算目前持有股數</summary>
    private async Task<int> CalculateCurrentHoldingsAsync(
        string userId, string stockSymbol, CancellationToken ct)
    {
        var totals = await db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.StockSymbol == stockSymbol)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalBuy = g.Where(t => t.TradeType == StockTradeType.Buy).Sum(t => t.Shares),
                TotalSell = g.Where(t => t.TradeType == StockTradeType.Sell).Sum(t => t.Shares),
            })
            .FirstOrDefaultAsync(ct);

        return (totals?.TotalBuy ?? 0) - (totals?.TotalSell ?? 0);
    }

    /// <summary>計算所有賣出交易的已實現損益合計（加權平均成本法）</summary>
    private static decimal CalculateTotalRealizedPL(List<StockTransaction> transactions)
    {
        var groups = transactions.GroupBy(t => t.StockSymbol);
        var totalPL = 0m;

        foreach (var group in groups)
        {
            var totalCost = 0m;
            var totalShares = 0;

            foreach (var t in group.OrderBy(t => t.TradeDate).ThenBy(t => t.CreatedAt))
            {
                var amount = t.Shares * t.PricePerShare;

                if (t.TradeType == StockTradeType.Buy)
                {
                    totalCost += amount + t.Commission;
                    totalShares += t.Shares;
                }
                else if (totalShares > 0)
                {
                    var avgCost = totalCost / totalShares;
                    var costOfSold = avgCost * t.Shares;
                    totalCost -= costOfSold;
                    totalShares -= t.Shares;
                    totalPL += (amount - t.Commission - t.Tax) - costOfSold;
                }
            }
        }

        return totalPL;
    }

    /// <summary>計算單筆賣出交易的已實現損益</summary>
    private async Task<decimal> CalculateRealizedPLAsync(
        string userId, StockTransaction sellTransaction, CancellationToken ct)
    {
        var allTransactions = await db.StockTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.StockSymbol == sellTransaction.StockSymbol)
            .OrderBy(t => t.TradeDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var totalCost = 0m;
        var totalShares = 0;

        foreach (var t in allTransactions)
        {
            var amount = t.Shares * t.PricePerShare;

            if (t.TradeType == StockTradeType.Buy)
            {
                totalCost += amount + t.Commission;
                totalShares += t.Shares;
            }
            else
            {
                if (totalShares > 0)
                {
                    var avgCost = totalCost / totalShares;
                    var costOfSold = avgCost * t.Shares;
                    totalCost -= costOfSold;
                    totalShares -= t.Shares;

                    if (t.Id == sellTransaction.Id)
                    {
                        return (amount - t.Commission - t.Tax) - costOfSold;
                    }
                }
            }
        }

        return 0m;
    }

    private static StockTransactionResponse MapToResponse(StockTransaction t)
    {
        var totalAmount = t.Shares * t.PricePerShare;
        var netAmount = t.TradeType == StockTradeType.Buy
            ? totalAmount + t.Commission
            : totalAmount - t.Commission - t.Tax;

        return new StockTransactionResponse(
            t.Id,
            t.StockSymbol,
            t.StockName,
            t.TradeType.ToString(),
            t.Shares,
            t.PricePerShare,
            totalAmount,
            t.Commission,
            t.Tax,
            netAmount,
            null,
            t.TradeDate,
            t.Note);
    }

    private async Task<StockTransactionResponse> MapToResponseWithPL(
        StockTransaction t, string userId, CancellationToken ct)
    {
        var totalAmount = t.Shares * t.PricePerShare;
        var netAmount = totalAmount - t.Commission - t.Tax;
        decimal? realizedPL = null;

        if (t.TradeType == StockTradeType.Sell)
        {
            realizedPL = Math.Round(await CalculateRealizedPLAsync(userId, t, ct), 2);
        }

        return new StockTransactionResponse(
            t.Id,
            t.StockSymbol,
            t.StockName,
            t.TradeType.ToString(),
            t.Shares,
            t.PricePerShare,
            totalAmount,
            t.Commission,
            t.Tax,
            netAmount,
            realizedPL,
            t.TradeDate,
            t.Note);
    }

    /// <summary>持股計算狀態（內部使用）</summary>
    private class HoldingState
    {
        public string StockName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public int TotalShares { get; set; }
        public decimal TotalRealizedPL { get; set; }
    }
}
