using HansOS.Api.Models.Finance;

namespace HansOS.Api.Services;

public interface IFinanceAccountService
{
    /// <summary>取得使用者所有帳戶</summary>
    Task<List<AccountResponse>> GetAccountsAsync(string userId, CancellationToken ct = default);

    /// <summary>建立帳戶</summary>
    Task<AccountResponse> CreateAccountAsync(string userId, CreateAccountRequest request, CancellationToken ct = default);

    /// <summary>更新帳戶</summary>
    Task<AccountResponse> UpdateAccountAsync(string userId, Guid accountId, UpdateAccountRequest request, CancellationToken ct = default);

    /// <summary>刪除帳戶</summary>
    Task DeleteAccountAsync(string userId, Guid accountId, CancellationToken ct = default);

    /// <summary>取得帳戶餘額（含計算後的目前餘額）</summary>
    Task<List<AccountBalanceResponse>> GetAccountBalancesAsync(string userId, CancellationToken ct = default);
}
