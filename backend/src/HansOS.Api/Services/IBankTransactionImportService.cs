using HansOS.Api.Models.BankTransactions;

namespace HansOS.Api.Services;

public interface IBankTransactionImportService
{
    Task<ImportResultResponse> ImportFromSeedDataAsync(CancellationToken ct = default);
}
