namespace HansOS.Api.Data.Entities;

/// <summary>股票交易類型</summary>
public enum StockTradeType
{
    /// <summary>買入</summary>
    Buy = 1,

    /// <summary>賣出</summary>
    Sell = 2,
}

/// <summary>股票交易記錄</summary>
public class StockTransaction
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>股票代號，如 "2330"</summary>
    public string StockSymbol { get; set; } = string.Empty;

    /// <summary>股票名稱，如 "台積電"</summary>
    public string StockName { get; set; } = string.Empty;

    public StockTradeType TradeType { get; set; }

    /// <summary>交易股數</summary>
    public int Shares { get; set; }

    /// <summary>每股成交價</summary>
    public decimal PricePerShare { get; set; }

    /// <summary>手續費</summary>
    public decimal Commission { get; set; }

    /// <summary>證券交易稅（僅賣出時）</summary>
    public decimal Tax { get; set; }

    public DateOnly TradeDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
