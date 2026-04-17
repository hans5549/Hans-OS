using HansOS.Api.Models.BankTransactions;

namespace HansOS.Api.Services;

public interface IBankTransactionReceiptService
{
    Task<ReceiptTrackingSummaryResponse> GetReceiptTrackingAsync(
        int year, int? month = null, CancellationToken ct = default);

    Task PatchReceiptStatusAsync(
        Guid id, PatchReceiptStatusRequest request, CancellationToken ct = default);
}
