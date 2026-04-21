using HansOS.Api.Data.Entities;
using HansOS.Api.Models.FinanceTasks;

namespace HansOS.Api.Services;

public interface IFinanceTaskService
{
    Task<List<FinanceTaskResponse>> GetAllAsync(
        FinanceTaskStatus? status = null, CancellationToken ct = default);

    Task<FinanceTaskResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<FinanceTaskResponse> CreateAsync(
        CreateFinanceTaskRequest request, CancellationToken ct = default);

    Task UpdateAsync(
        Guid id, UpdateFinanceTaskRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task CompleteAsync(Guid id, CancellationToken ct = default);
}
