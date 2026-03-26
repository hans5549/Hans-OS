using HansOS.Api.Models.AnnualBudget;

namespace HansOS.Api.Services;

public interface IAnnualBudgetService
{
    /// <summary>取得年度預算總覽（自動初始化）</summary>
    Task<AnnualBudgetOverviewResponse> GetOverviewAsync(int year, CancellationToken ct = default);

    /// <summary>更新預算狀態</summary>
    Task UpdateStatusAsync(int year, string status, CancellationToken ct = default);

    /// <summary>取得部門預算項目</summary>
    Task<List<BudgetItemResponse>> GetDepartmentItemsAsync(
        int year, Guid departmentId, CancellationToken ct = default);

    /// <summary>批次儲存部門預算項目</summary>
    Task<List<BudgetItemResponse>> SaveDepartmentItemsAsync(
        int year, Guid departmentId, SaveBudgetItemsRequest request, CancellationToken ct = default);
}
