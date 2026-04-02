using HansOS.Api.Models.AnnualBudget;
using HansOS.Api.Models.BudgetShare;

namespace HansOS.Api.Services;

public interface IBudgetShareService
{
    // ── 管理端（需認證）──────────────────────────

    /// <summary>建立或重新產生分享 Token</summary>
    Task<BudgetShareInfoResponse> CreateShareTokenAsync(
        int year, Guid departmentId, CancellationToken ct = default);

    /// <summary>取得分享資訊</summary>
    Task<BudgetShareInfoResponse?> GetShareInfoAsync(
        int year, Guid departmentId, CancellationToken ct = default);

    /// <summary>更新分享設定（權限、啟用狀態）</summary>
    Task<BudgetShareInfoResponse> UpdateShareAsync(
        int year, Guid departmentId, UpdateShareRequest request, CancellationToken ct = default);

    /// <summary>撤銷分享 Token</summary>
    Task RevokeShareAsync(int year, Guid departmentId, CancellationToken ct = default);

    // ── 公開端（透過 Token）─────────────────────

    /// <summary>透過 Token 取得預算資料</summary>
    Task<PublicBudgetResponse> GetBudgetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>透過 Token 儲存預算項目</summary>
    Task<List<PublicBudgetItemResponse>> SaveItemsByTokenAsync(
        string token, PublicSaveBudgetItemsRequest request, CancellationToken ct = default);
}
