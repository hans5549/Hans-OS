using HansOS.Api.Models.Activities;

namespace HansOS.Api.Services;

public interface IActivityService
{
    /// <summary>取得活動列表（含摘要資訊）</summary>
    Task<List<ActivitySummaryResponse>> GetListAsync(
        int year, int? month, Guid? departmentId, CancellationToken ct = default);

    /// <summary>取得各月活動統計</summary>
    Task<List<MonthSummaryResponse>> GetMonthSummariesAsync(
        int year, Guid? departmentId, CancellationToken ct = default);

    /// <summary>取得活動完整明細</summary>
    Task<ActivityDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>新增活動</summary>
    Task<ActivityDetailResponse> CreateAsync(
        CreateActivityRequest request, CancellationToken ct = default);

    /// <summary>更新活動</summary>
    Task<ActivityDetailResponse> UpdateAsync(
        Guid id, UpdateActivityRequest request, CancellationToken ct = default);

    /// <summary>刪除活動</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
