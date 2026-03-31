using HansOS.Api.Data.Entities;
using HansOS.Api.Models.PendingRemittances;

namespace HansOS.Api.Services;

public interface IPendingRemittanceService
{
    Task<List<PendingRemittanceResponse>> GetAllAsync(
        PendingRemittanceStatus? status = null, CancellationToken ct = default);

    Task<PendingRemittanceResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PendingRemittanceResponse> CreateAsync(
        CreatePendingRemittanceRequest request, CancellationToken ct = default);

    Task UpdateAsync(
        Guid id, UpdatePendingRemittanceRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task CompleteAsync(Guid id, CancellationToken ct = default);
}
