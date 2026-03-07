namespace CGMSportFinance.Api.Common;

public sealed record ApiEnvelope<T>(int Code, T? Data, string? Error, string Message)
{
    public static ApiEnvelope<T> Success(T data, string message = "ok") => new(0, data, null, message);
}
