using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HansOS.Api.Common.Observability;

/// <summary>
/// EF Core 慢查詢偵測 Interceptor。
/// 當 SQL command 執行超過 <see cref="ThresholdMilliseconds"/> 時輸出 Warning 日誌。
/// </summary>
public class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    public const int ThresholdMilliseconds = 500;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        var elapsedMs = eventData.Duration.TotalMilliseconds;
        if (elapsedMs < ThresholdMilliseconds)
        {
            return;
        }

        logger.LogWarning(
            "[SlowQuery] {ElapsedMs:F0}ms {CommandText}",
            elapsedMs,
            command.CommandText);
    }
}
