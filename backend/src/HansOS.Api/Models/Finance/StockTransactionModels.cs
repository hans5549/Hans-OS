using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

public record BuyStockRequest(
    [Required][StringLength(20)] string StockSymbol,
    [Required][StringLength(100)] string StockName,
    [Required][Range(1, int.MaxValue)] int Shares,
    [Required][Range(0.01, double.MaxValue)] decimal PricePerShare,
    [Range(0, 1)] decimal CommissionDiscount = 0.6m,  // 券商手續費折扣（預設6折）
    [Required] DateOnly TradeDate = default,
    [StringLength(500)] string? Note = null);

public record SellStockRequest(
    [Required][StringLength(20)] string StockSymbol,
    [Required][Range(1, int.MaxValue)] int Shares,
    [Required][Range(0.01, double.MaxValue)] decimal PricePerShare,
    [Range(0, 1)] decimal CommissionDiscount = 0.6m,
    bool IsEtf = false,  // ETF 交易稅 0.1%, 一般股票 0.3%
    [Required] DateOnly TradeDate = default,
    [StringLength(500)] string? Note = null);

public record StockTransactionResponse(
    Guid Id,
    string StockSymbol,
    string StockName,
    string TradeType,  // Buy, Sell
    int Shares,
    decimal PricePerShare,
    decimal TotalAmount,       // Shares * PricePerShare
    decimal Commission,
    decimal Tax,
    decimal NetAmount,         // Buy: TotalAmount + Commission; Sell: TotalAmount - Commission - Tax
    decimal? RealizedProfitLoss,  // Only for Sell: NetProceeds - AverageCost * Shares
    DateOnly TradeDate,
    string? Note);

/// <summary>Current holding for a stock</summary>
public record StockHoldingResponse(
    string StockSymbol,
    string StockName,
    int Shares,
    decimal AverageCost,       // 加權平均成本
    decimal TotalCost,         // 累計持有成本
    decimal TotalRealizedPL);  // 累計已實現損益

/// <summary>Annual or overall profit summary</summary>
public record StockProfitSummaryResponse(
    decimal TotalRealizedPL,
    decimal TotalCommission,
    decimal TotalTax,
    int TransactionCount);
