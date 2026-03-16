using System.Text.Json.Serialization;

namespace HansOS.Api.Models.Menus;

public class MenuMetaResponse
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    [JsonPropertyName("activeIcon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActiveIcon { get; init; }

    [JsonPropertyName("order")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Order { get; init; }

    [JsonPropertyName("affixTab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AffixTab { get; init; }

    [JsonPropertyName("affixTabOrder")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AffixTabOrder { get; init; }

    [JsonPropertyName("hideInMenu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HideInMenu { get; init; }

    [JsonPropertyName("hideInTab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HideInTab { get; init; }

    [JsonPropertyName("hideInBreadcrumb")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HideInBreadcrumb { get; init; }

    [JsonPropertyName("hideChildrenInMenu")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HideChildrenInMenu { get; init; }

    [JsonPropertyName("keepAlive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool KeepAlive { get; init; }

    [JsonPropertyName("authority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Authority { get; init; }

    [JsonPropertyName("badge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Badge { get; init; }

    [JsonPropertyName("badgeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BadgeType { get; init; }

    [JsonPropertyName("badgeVariants")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BadgeVariants { get; init; }

    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; init; }

    [JsonPropertyName("iframeSrc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IframeSrc { get; init; }

    [JsonPropertyName("noBasicLayout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NoBasicLayout { get; init; }

    [JsonPropertyName("maxNumOfOpenTab")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxNumOfOpenTab { get; init; }
}
