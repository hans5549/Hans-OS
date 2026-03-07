using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CGMSportFinance.Secrets;

public static class EncryptedSecretsSerializer
{
    private const string CurrentAlgorithm = "AES-256-GCM";
    private const string CurrentVersion = "1";
    private const int AesKeySize = 32;
    private const int Iterations = 100_000;
    private const int NonceSize = 12;
    private const int SaltSize = 16;
    private const int TagSize = 16;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string EncryptJson(string plainJson, string? passphrase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainJson);

        // Validate the payload early so invalid JSON never gets encrypted into the repo.
        using var _ = JsonDocument.Parse(plainJson);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plainJson);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];
        var key = DeriveKey(passphrase, salt);

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }

        var ciphertextWithTag = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, ciphertextWithTag, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, ciphertextWithTag, ciphertext.Length, tag.Length);

        return JsonSerializer.Serialize(
            new EncryptedSecretsDocument
            {
                Algorithm = CurrentAlgorithm,
                Ciphertext = Convert.ToBase64String(ciphertextWithTag),
                Nonce = Convert.ToBase64String(nonce),
                Salt = Convert.ToBase64String(salt),
                Version = CurrentVersion,
            },
            SerializerOptions);
    }

    public static string DecryptJson(string encryptedJson, string? passphrase = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedJson);

        EncryptedSecretsDocument document;
        try
        {
            document = JsonSerializer.Deserialize<EncryptedSecretsDocument>(encryptedJson, SerializerOptions)
                       ?? throw new InvalidOperationException("Encrypted secrets document is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Encrypted secrets document is not valid JSON.", ex);
        }

        if (!string.Equals(document.Version, CurrentVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported encrypted secrets version '{document.Version}'.");
        }

        if (!string.Equals(document.Algorithm, CurrentAlgorithm, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported encrypted secrets algorithm '{document.Algorithm}'.");
        }

        var salt = DecodeBase64(document.Salt, "salt");
        var nonce = DecodeBase64(document.Nonce, "nonce");
        var ciphertextWithTag = DecodeBase64(document.Ciphertext, "ciphertext");

        if (salt.Length != SaltSize)
        {
            throw new InvalidOperationException("Encrypted secrets salt is invalid.");
        }

        if (nonce.Length != NonceSize)
        {
            throw new InvalidOperationException("Encrypted secrets nonce is invalid.");
        }

        if (ciphertextWithTag.Length <= TagSize)
        {
            throw new InvalidOperationException("Encrypted secrets ciphertext is invalid.");
        }

        var ciphertext = ciphertextWithTag[..^TagSize];
        var tag = ciphertextWithTag[^TagSize..];
        var plaintext = new byte[ciphertext.Length];
        var key = DeriveKey(passphrase, salt);

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encrypted secrets could not be decrypted.", ex);
        }

        var plainJson = Encoding.UTF8.GetString(plaintext);

        try
        {
            using var _ = JsonDocument.Parse(plainJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Decrypted secrets payload is not valid JSON.", ex);
        }

        return plainJson;
    }

    private static byte[] DecodeBase64(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Encrypted secrets {propertyName} is missing.");
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Encrypted secrets {propertyName} is not valid Base64.", ex);
        }
    }

    private static byte[] DeriveKey(string? passphrase, byte[] salt)
    {
        var effectivePassphrase = string.IsNullOrWhiteSpace(passphrase)
            ? EmbeddedSecretsKey.Passphrase
            : passphrase;

        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(effectivePassphrase),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            AesKeySize);
    }
}
