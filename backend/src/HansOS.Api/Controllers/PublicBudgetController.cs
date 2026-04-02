using HansOS.Api.Common;
using HansOS.Api.Models.BudgetShare;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("public/department-budget")]
public class PublicBudgetController(IBudgetShareService shareService) : ControllerBase
{
    /// <summary>透過 Token 取得部門總覽（部門名稱 + 可用年度）</summary>
    [HttpGet("{token}")]
    public async Task<ApiEnvelope<DepartmentShareOverviewResponse>> GetOverview(
        string token, CancellationToken ct)
        => ApiEnvelope<DepartmentShareOverviewResponse>.Success(
            await shareService.GetDepartmentOverviewByTokenAsync(token, ct));

    /// <summary>透過 Token 取得特定年度預算資料</summary>
    [HttpGet("{token}/years/{year:int}")]
    public async Task<ApiEnvelope<PublicBudgetResponse>> GetBudget(
        string token, int year, CancellationToken ct)
        => ApiEnvelope<PublicBudgetResponse>.Success(
            await shareService.GetBudgetByTokenAsync(token, year, ct));

    /// <summary>透過 Token 儲存特定年度預算項目</summary>
    [HttpPut("{token}/years/{year:int}/items")]
    public async Task<ApiEnvelope<List<PublicBudgetItemResponse>>> SaveItems(
        string token, int year, [FromBody] PublicSaveBudgetItemsRequest request, CancellationToken ct)
        => ApiEnvelope<List<PublicBudgetItemResponse>>.Success(
            await shareService.SaveItemsByTokenAsync(token, year, request, ct));
}
