using HansOS.Api.Common;
using HansOS.Api.Models.TsfSettings;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("tsf-settings")]
[Authorize]
public class TsfSettingsController(ITsfSettingsService settingsService) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<ApiEnvelope<List<DepartmentResponse>>> GetDepartments(CancellationToken ct)
    {
        var result = await settingsService.GetDepartmentsAsync(ct);
        return ApiEnvelope<List<DepartmentResponse>>.Success(result);
    }

    [HttpPost("departments")]
    public async Task<ApiEnvelope<DepartmentResponse>> CreateDepartment(
        [FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await settingsService.CreateDepartmentAsync(request, ct);
        return ApiEnvelope<DepartmentResponse>.Success(result);
    }

    [HttpPut("departments/{id:guid}")]
    public async Task<ApiEnvelope<object?>> UpdateDepartment(
        Guid id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct)
    {
        await settingsService.UpdateDepartmentAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpDelete("departments/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteDepartment(Guid id, CancellationToken ct)
    {
        await settingsService.DeleteDepartmentAsync(id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpGet("bank-balances")]
    public async Task<ApiEnvelope<List<BankInitialBalanceResponse>>> GetBankBalances(CancellationToken ct)
    {
        var result = await settingsService.GetBankInitialBalancesAsync(ct);
        return ApiEnvelope<List<BankInitialBalanceResponse>>.Success(result);
    }

    [HttpPut("bank-balances/{id:guid}")]
    public async Task<ApiEnvelope<object?>> UpdateBankBalance(
        Guid id, [FromBody] UpdateBankInitialBalanceRequest request, CancellationToken ct)
    {
        await settingsService.UpdateBankInitialBalanceAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
