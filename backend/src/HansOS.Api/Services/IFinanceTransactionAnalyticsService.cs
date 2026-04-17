using HansOS.Api.Models.Finance;

namespace HansOS.Api.Services;

/// <summary>個人記帳分析查詢服務（月度摘要、趨勢、分類佔比）</summary>
public interface IFinanceTransactionAnalyticsService
{
    /// <summary>取得指定月份的收支摘要</summary>
    Task<MonthlySummaryResponse> GetMonthlySummaryAsync(string userId, int year, int month, CancellationToken ct = default);

    /// <summary>取得跨月收支趨勢（最多 12 個月）</summary>
    Task<TrendResponse> GetTrendAsync(string userId, int startYear, int startMonth, int endYear, int endMonth, CancellationToken ct = default);

    /// <summary>取得月度分類佔比</summary>
    Task<CategoryBreakdownResponse> GetCategoryBreakdownAsync(string userId, int year, int month, string type, CancellationToken ct = default);
}
