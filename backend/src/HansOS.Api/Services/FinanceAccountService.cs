using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>個人記帳帳戶服務</summary>
public class FinanceAccountService(
    ApplicationDbContext db,
    ILogger<FinanceAccountService> logger) : IFinanceAccountService
{
    /// <inheritdoc />
    public async Task<List<AccountResponse>> GetAccountsAsync(
        string userId, CancellationToken ct = default)
    {
        return await db.FinanceAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .Select(a => new AccountResponse(
                a.Id,
                a.Name,
                a.AccountType.ToString(),
                a.InitialBalance,
                a.Icon,
                a.SortOrder,
                a.IsArchived))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<AccountResponse> CreateAccountAsync(
        string userId, CreateAccountRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<FinanceAccountType>(request.AccountType, ignoreCase: true, out var accountType))
        {
            throw new ArgumentException("無效的帳戶類型");
        }

        var nameExists = await db.FinanceAccounts
            .AnyAsync(a => a.UserId == userId && a.Name == request.Name, ct);

        if (nameExists)
        {
            throw new ArgumentException($"帳戶名稱「{request.Name}」已存在");
        }

        var now = DateTime.UtcNow;
        var account = new FinanceAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            AccountType = accountType,
            InitialBalance = request.InitialBalance,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.FinanceAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("帳戶已建立: {AccountId}, 名稱={Name}", account.Id, account.Name);

        return new AccountResponse(
            account.Id,
            account.Name,
            account.AccountType.ToString(),
            account.InitialBalance,
            account.Icon,
            account.SortOrder,
            account.IsArchived);
    }

    /// <inheritdoc />
    public async Task<AccountResponse> UpdateAccountAsync(
        string userId, Guid accountId, UpdateAccountRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<FinanceAccountType>(request.AccountType, ignoreCase: true, out var accountType))
        {
            throw new ArgumentException("無效的帳戶類型");
        }

        var account = await db.FinanceAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("帳戶不存在");

        var nameExists = await db.FinanceAccounts
            .AnyAsync(a => a.UserId == userId && a.Name == request.Name && a.Id != accountId, ct);

        if (nameExists)
        {
            throw new ArgumentException($"帳戶名稱「{request.Name}」已存在");
        }

        account.Name = request.Name;
        account.AccountType = accountType;
        account.InitialBalance = request.InitialBalance;
        account.Icon = request.Icon;
        account.SortOrder = request.SortOrder;
        account.IsArchived = request.IsArchived;
        account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("帳戶已更新: {AccountId}", accountId);

        return new AccountResponse(
            account.Id,
            account.Name,
            account.AccountType.ToString(),
            account.InitialBalance,
            account.Icon,
            account.SortOrder,
            account.IsArchived);
    }

    /// <inheritdoc />
    public async Task DeleteAccountAsync(
        string userId, Guid accountId, CancellationToken ct = default)
    {
        var account = await db.FinanceAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("帳戶不存在");

        var hasTransactions = await db.FinanceTransactions
            .AnyAsync(t => t.UserId == userId && (t.AccountId == accountId || t.ToAccountId == accountId), ct);

        if (hasTransactions)
        {
            throw new ArgumentException("此帳戶已有交易記錄，無法刪除");
        }

        db.FinanceAccounts.Remove(account);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("帳戶已刪除: {AccountId}", accountId);
    }

    /// <inheritdoc />
    public async Task<List<AccountBalanceResponse>> GetAccountBalancesAsync(
        string userId, CancellationToken ct = default)
    {
        var accounts = await db.FinanceAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Name)
            .ToListAsync(ct);

        var transactions = await db.FinanceTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new
            {
                t.TransactionType,
                t.Amount,
                t.AccountId,
                t.ToAccountId,
            })
            .ToListAsync(ct);

        return accounts.Select(a =>
        {
            var currentBalance = a.InitialBalance;

            foreach (var t in transactions)
            {
                if (t.TransactionType == FinanceTransactionType.Income && t.AccountId == a.Id)
                {
                    currentBalance += t.Amount;
                }
                else if (t.TransactionType == FinanceTransactionType.Expense && t.AccountId == a.Id)
                {
                    currentBalance -= t.Amount;
                }
                else if (t.TransactionType == FinanceTransactionType.Transfer)
                {
                    if (t.ToAccountId == a.Id)
                    {
                        currentBalance += t.Amount;
                    }

                    if (t.AccountId == a.Id)
                    {
                        currentBalance -= t.Amount;
                    }
                }
            }

            return new AccountBalanceResponse(
                a.Id,
                a.Name,
                a.AccountType.ToString(),
                a.InitialBalance,
                currentBalance,
                a.Icon,
                a.IsArchived);
        }).ToList();
    }
}
