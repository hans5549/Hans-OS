using HansOS.Api.Common;
using HansOS.Api.Models.Activities;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("activities")]
[Authorize]
public class ActivityController(IActivityService activityService) : ControllerBase
{
    /// <summary>取得活動列表</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<ActivitySummaryResponse>>> GetList(
        [FromQuery] int year,
        [FromQuery] int? month,
        [FromQuery] Guid? departmentId,
        CancellationToken ct)
        => ApiEnvelope<List<ActivitySummaryResponse>>.Success(
            await activityService.GetListAsync(year, month, departmentId, ct));

    /// <summary>取得各月活動統計</summary>
    [HttpGet("month-summaries")]
    public async Task<ApiEnvelope<List<MonthSummaryResponse>>> GetMonthSummaries(
        [FromQuery] int year,
        [FromQuery] Guid? departmentId,
        CancellationToken ct)
        => ApiEnvelope<List<MonthSummaryResponse>>.Success(
            await activityService.GetMonthSummariesAsync(year, departmentId, ct));

    /// <summary>取得活動明細</summary>
    [HttpGet("{id:guid}")]
    public async Task<ApiEnvelope<ActivityDetailResponse>> GetDetail(
        Guid id, CancellationToken ct)
        => ApiEnvelope<ActivityDetailResponse>.Success(
            await activityService.GetDetailAsync(id, ct));

    /// <summary>新增活動</summary>
    [HttpPost]
    public async Task<ApiEnvelope<ActivityDetailResponse>> Create(
        [FromBody] CreateActivityRequest request, CancellationToken ct)
        => ApiEnvelope<ActivityDetailResponse>.Success(
            await activityService.CreateAsync(request, ct));

    /// <summary>更新活動</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<ActivityDetailResponse>> Update(
        Guid id, [FromBody] UpdateActivityRequest request, CancellationToken ct)
        => ApiEnvelope<ActivityDetailResponse>.Success(
            await activityService.UpdateAsync(id, request, ct));

    /// <summary>刪除活動</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> Delete(Guid id, CancellationToken ct)
    {
        await activityService.DeleteAsync(id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
