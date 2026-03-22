using System.Text.Json.Serialization;

namespace HansOS.Api.Models.Menus;

public class MenuRouteResponse
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("component")]
    public string Component { get; init; } = string.Empty;

    [JsonPropertyName("redirect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Redirect { get; init; }

    [JsonPropertyName("meta")]
    public MenuMetaResponse Meta { get; init; } = new();

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MenuRouteResponse>? Children { get; init; }
}
