using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace HansOS.Api.Common;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error) = exception switch
        {
            ForbiddenException e => (HttpStatusCode.Forbidden, e.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授權"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "資源不存在"),
            ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
            _ => (HttpStatusCode.InternalServerError, "伺服器內部錯誤")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "未處理的例外: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "已處理的例外 ({StatusCode}): {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = (int)statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiEnvelope<object>.Fail(error),
            cancellationToken);

        return true;
    }
}
