using HansOS.Api.Models.BankTransactions;

namespace HansOS.Api.Services;

public interface IBankTransactionService
{
    Task<List<BankTransactionResponse>> GetTransactionsAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default);

    Task<BankTransactionSummaryResponse> GetPeriodSummaryAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default);

    Task<BankTransactionResponse> CreateTransactionAsync(
        string bankName, CreateBankTransactionRequest request, CancellationToken ct = default);

    Task UpdateTransactionAsync(
        Guid id, UpdateBankTransactionRequest request, CancellationToken ct = default);

    Task DeleteTransactionAsync(Guid id, CancellationToken ct = default);

    Task<byte[]> ExportToExcelAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default);

    Task<ReceiptTrackingSummaryResponse> GetReceiptTrackingAsync(
        int year, int? month = null, CancellationToken ct = default);
}
