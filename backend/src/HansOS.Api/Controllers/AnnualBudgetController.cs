using HansOS.Api.Common;
using HansOS.Api.Models.AnnualBudget;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("annual-budgets")]
[Authorize]
public class AnnualBudgetController(IAnnualBudgetService budgetService) : ControllerBase
{
    /// <summary>取得年度預算總覽（自動初始化）</summary>
    [HttpGet("{year:int}")]
    public async Task<ApiEnvelope<AnnualBudgetOverviewResponse>> GetOverview(
        int year, CancellationToken ct)
        => ApiEnvelope<AnnualBudgetOverviewResponse>.Success(
            await budgetService.GetOverviewAsync(year, ct));

    /// <summary>更新預算狀態</summary>
    [HttpPut("{year:int}/status")]
    public async Task<ApiEnvelope<object?>> UpdateStatus(
        int year, [FromBody] UpdateBudgetStatusRequest request, CancellationToken ct)
    {
        await budgetService.UpdateStatusAsync(year, request.Status, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    /// <summary>取得部門預算項目</summary>
    [HttpGet("{year:int}/departments/{departmentId:guid}/items")]
    public async Task<ApiEnvelope<List<BudgetItemResponse>>> GetDepartmentItems(
        int year, Guid departmentId, CancellationToken ct)
        => ApiEnvelope<List<BudgetItemResponse>>.Success(
            await budgetService.GetDepartmentItemsAsync(year, departmentId, ct));

    /// <summary>批次儲存部門預算項目</summary>
    [HttpPut("{year:int}/departments/{departmentId:guid}/items")]
    public async Task<ApiEnvelope<List<BudgetItemResponse>>> SaveDepartmentItems(
        int year, Guid departmentId, [FromBody] SaveBudgetItemsRequest request, CancellationToken ct)
        => ApiEnvelope<List<BudgetItemResponse>>.Success(
            await budgetService.SaveDepartmentItemsAsync(year, departmentId, request, ct));
}
