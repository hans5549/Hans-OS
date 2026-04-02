using HansOS.Api.Models.Finance;

namespace HansOS.Api.Services;

public interface IFinanceTransactionService
{
    /// <summary>取得指定月份的交易記錄（依日期分組）</summary>
    Task<List<DailyTransactionGroup>> GetTransactionsAsync(string userId, int year, int month, CancellationToken ct = default);

    /// <summary>取得指定月份的收支摘要</summary>
    Task<MonthlySummaryResponse> GetMonthlySummaryAsync(string userId, int year, int month, CancellationToken ct = default);

    /// <summary>建立交易記錄</summary>
    Task<TransactionResponse> CreateTransactionAsync(string userId, CreateTransactionRequest request, CancellationToken ct = default);

    /// <summary>更新交易記錄</summary>
    Task<TransactionResponse> UpdateTransactionAsync(string userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken ct = default);

    /// <summary>刪除交易記錄</summary>
    Task DeleteTransactionAsync(string userId, Guid transactionId, CancellationToken ct = default);
}
