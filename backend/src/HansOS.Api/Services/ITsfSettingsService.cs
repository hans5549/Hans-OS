using HansOS.Api.Models.TsfSettings;

namespace HansOS.Api.Services;

public interface ITsfSettingsService
{
    Task<List<DepartmentResponse>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<DepartmentResponse> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct = default);
    Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default);

    Task<List<BankInitialBalanceResponse>> GetBankInitialBalancesAsync(CancellationToken ct = default);
    Task UpdateBankInitialBalanceAsync(Guid id, UpdateBankInitialBalanceRequest request, CancellationToken ct = default);
}
