using System.Text.Json.Serialization;

namespace CGMSportFinance.Api.Features.Menus.Contracts;

public sealed class MenuRouteResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MenuRouteResponse>? Children { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Component { get; init; }

    public MenuMetaResponse Meta { get; init; } = new();

    public string Name { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Redirect { get; init; }
}
