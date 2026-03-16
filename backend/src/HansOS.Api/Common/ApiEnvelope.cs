using System.Text.Json.Serialization;

namespace HansOS.Api.Common;

public class ApiEnvelope<T>
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    public static ApiEnvelope<T> Success(T data, string message = "ok") => new()
    {
        Code = 0,
        Data = data,
        Error = null,
        Message = message
    };

    public static ApiEnvelope<T> Fail(string error, string? message = null) => new()
    {
        Code = -1,
        Data = default,
        Error = error,
        Message = message ?? error
    };
}
