using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Finance;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>帳戶管理 API</summary>
[ApiController]
[Route("finance/accounts")]
[Authorize]
public class FinanceAccountController(IFinanceAccountService accountService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得所有帳戶</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<AccountResponse>>> GetAccounts(CancellationToken ct)
        => ApiEnvelope<List<AccountResponse>>.Success(
            await accountService.GetAccountsAsync(CurrentUserId, ct));

    /// <summary>新增帳戶</summary>
    [HttpPost]
    public async Task<ApiEnvelope<AccountResponse>> CreateAccount(
        [FromBody] CreateAccountRequest request, CancellationToken ct)
        => ApiEnvelope<AccountResponse>.Success(
            await accountService.CreateAccountAsync(CurrentUserId, request, ct));

    /// <summary>更新帳戶</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<AccountResponse>> UpdateAccount(
        Guid id, [FromBody] UpdateAccountRequest request, CancellationToken ct)
        => ApiEnvelope<AccountResponse>.Success(
            await accountService.UpdateAccountAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除帳戶</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteAccount(Guid id, CancellationToken ct)
    {
        await accountService.DeleteAccountAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    /// <summary>取得帳戶餘額</summary>
    [HttpGet("balances")]
    public async Task<ApiEnvelope<List<AccountBalanceResponse>>> GetBalances(CancellationToken ct)
        => ApiEnvelope<List<AccountBalanceResponse>>.Success(
            await accountService.GetAccountBalancesAsync(CurrentUserId, ct));
}
