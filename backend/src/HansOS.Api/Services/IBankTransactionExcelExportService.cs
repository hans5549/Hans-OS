namespace HansOS.Api.Services;

public interface IBankTransactionExcelExportService
{
    Task<byte[]> ExportAsync(
        string bankName, int year, int? month = null, CancellationToken ct = default);
}
