using HansOS.Api.Data.Entities;
using HansOS.Api.Models.FinanceTasks;

namespace HansOS.Api.Services;

public interface IUnifiedTaskService
{
    Task<UnifiedTaskListResponse> GetUnifiedTasksAsync(
        int? year = null,
        int? month = null,
        FinanceTaskStatus? status = null,
        UnifiedTaskType? type = null,
        CancellationToken ct = default);
}
