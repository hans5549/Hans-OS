using System.Text.Json;
using CGMSportFinance.Secrets;

namespace CGMSportFinance.Api.Infrastructure.Configuration;

public sealed class EncryptedConfigurationProvider(EncryptedConfigurationSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var encryptedDocument = reader.ReadToEnd();
        var plainJson = EncryptedSecretsSerializer.DecryptJson(encryptedDocument);

        Data = FlattenJson(plainJson);
    }

    private static Dictionary<string, string?> FlattenJson(string plainJson)
    {
        using var document = JsonDocument.Parse(plainJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Decrypted secrets payload must be a JSON object.");
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        VisitObject(document.RootElement, parentPath: null, data);
        return data;
    }

    private static void VisitObject(JsonElement element, string? parentPath, IDictionary<string, string?> data)
    {
        foreach (var property in element.EnumerateObject())
        {
            var currentPath = string.IsNullOrWhiteSpace(parentPath)
                ? property.Name
                : $"{parentPath}:{property.Name}";

            VisitValue(property.Value, currentPath, data);
        }
    }

    private static void VisitArray(JsonElement element, string parentPath, IDictionary<string, string?> data)
    {
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            VisitValue(item, $"{parentPath}:{index}", data);
            index++;
        }
    }

    private static void VisitValue(JsonElement element, string currentPath, IDictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObject(element, currentPath, data);
                break;
            case JsonValueKind.Array:
                VisitArray(element, currentPath, data);
                break;
            case JsonValueKind.Null:
                data[currentPath] = null;
                break;
            case JsonValueKind.String:
                data[currentPath] = element.GetString();
                break;
            default:
                data[currentPath] = element.GetRawText();
                break;
        }
    }
}
