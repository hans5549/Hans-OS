using HansOS.Api.Common;
using HansOS.Api.Models.BudgetShare;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("public/budget")]
public class PublicBudgetController(IBudgetShareService shareService) : ControllerBase
{
    /// <summary>透過 Token 取得預算資料</summary>
    [HttpGet("{token}")]
    public async Task<ApiEnvelope<PublicBudgetResponse>> GetBudget(
        string token, CancellationToken ct)
        => ApiEnvelope<PublicBudgetResponse>.Success(
            await shareService.GetBudgetByTokenAsync(token, ct));

    /// <summary>透過 Token 儲存預算項目</summary>
    [HttpPut("{token}/items")]
    public async Task<ApiEnvelope<List<PublicBudgetItemResponse>>> SaveItems(
        string token, [FromBody] PublicSaveBudgetItemsRequest request, CancellationToken ct)
        => ApiEnvelope<List<PublicBudgetItemResponse>>.Success(
            await shareService.SaveItemsByTokenAsync(token, request, ct));
}
