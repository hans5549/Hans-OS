using System.Text.Json.Serialization;

namespace HansOS.Api.Models.Users;

public record UserInfoResponse(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("realName")] string RealName,
    [property: JsonPropertyName("avatar")] string Avatar,
    [property: JsonPropertyName("roles")] string[] Roles,
    [property: JsonPropertyName("desc")] string Desc,
    [property: JsonPropertyName("homePath")] string HomePath,
    [property: JsonPropertyName("token")] string Token);
