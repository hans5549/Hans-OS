using System.Text.Json;
using CGMSportFinance.Secrets;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class EncryptedSecretsSerializerTests
{
    [Fact]
    public void EncryptAndDecryptJson_RoundTripsPayload()
    {
        const string plainJson = """
                                 {
                                   "ConnectionStrings": {
                                     "DefaultConnection": "Host=encrypted;Port=5432;Database=sample;Username=user;Password=password"
                                   },
                                   "Jwt": {
                                     "SigningKey": "sample-signing-key"
                                   }
                                 }
                                 """;

        var encryptedJson = EncryptedSecretsSerializer.EncryptJson(plainJson);
        var decryptedJson = EncryptedSecretsSerializer.DecryptJson(encryptedJson);

        using var expected = JsonDocument.Parse(plainJson);
        using var actual = JsonDocument.Parse(decryptedJson);

        Assert.Equal(expected.RootElement.ToString(), actual.RootElement.ToString());
    }

    [Fact]
    public void DecryptJson_WithTamperedCiphertext_Throws()
    {
        const string plainJson = """
                                 {
                                   "Jwt": {
                                     "SigningKey": "sample-signing-key"
                                   }
                                 }
                                 """;

        var encryptedJson = EncryptedSecretsSerializer.EncryptJson(plainJson);
        var document = JsonSerializer.Deserialize<EncryptedSecretsDocument>(encryptedJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var ciphertext = Convert.FromBase64String(document.Ciphertext);
        ciphertext[0] ^= 0xFF;

        var tamperedJson = JsonSerializer.Serialize(document with
        {
            Ciphertext = Convert.ToBase64String(ciphertext),
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var exception = Assert.Throws<InvalidOperationException>(() => EncryptedSecretsSerializer.DecryptJson(tamperedJson));
        Assert.Contains("could not be decrypted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecryptJson_WithMalformedDocument_Throws()
    {
        const string malformedJson = """
                                     {
                                       "version": "1",
                                       "algorithm": "AES-256-GCM",
                                       "salt": "",
                                       "nonce": "",
                                       "ciphertext": ""
                                     }
                                     """;

        var exception = Assert.Throws<InvalidOperationException>(() => EncryptedSecretsSerializer.DecryptJson(malformedJson));
        Assert.Contains("salt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
