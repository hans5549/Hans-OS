using HansOS.Api.Models.BudgetShare;

namespace HansOS.Api.Services;

public interface IBudgetShareService
{
    // ── 管理端（需認證）──────────────────────────

    /// <summary>取得或自動建立部門分享 Token</summary>
    Task<DepartmentShareInfoResponse> GetOrCreateShareTokenAsync(
        Guid departmentId, CancellationToken ct = default);

    /// <summary>更新分享設定（權限、啟用狀態）</summary>
    Task<DepartmentShareInfoResponse> UpdateShareAsync(
        Guid departmentId, UpdateShareRequest request, CancellationToken ct = default);

    /// <summary>重新產生 Token（舊 Token 失效）</summary>
    Task<DepartmentShareInfoResponse> RegenerateTokenAsync(
        Guid departmentId, CancellationToken ct = default);

    /// <summary>撤銷分享 Token</summary>
    Task RevokeShareAsync(Guid departmentId, CancellationToken ct = default);

    // ── 公開端（透過 Token）─────────────────────

    /// <summary>透過 Token 取得部門總覽（部門名稱 + 可用年度）</summary>
    Task<DepartmentShareOverviewResponse> GetDepartmentOverviewByTokenAsync(
        string token, CancellationToken ct = default);

    /// <summary>透過 Token 取得特定年度預算資料</summary>
    Task<PublicBudgetResponse> GetBudgetByTokenAsync(
        string token, int year, CancellationToken ct = default);

    /// <summary>透過 Token 儲存特定年度預算項目</summary>
    Task<List<PublicBudgetItemResponse>> SaveItemsByTokenAsync(
        string token, int year, PublicSaveBudgetItemsRequest request, CancellationToken ct = default);
}
