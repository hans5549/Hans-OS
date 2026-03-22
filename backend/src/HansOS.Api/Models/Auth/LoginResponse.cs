using System.Text.Json.Serialization;

namespace HansOS.Api.Models.Auth;

public record LoginResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken);
