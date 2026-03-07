namespace CGMSportFinance.Secrets;

public sealed record EncryptedSecretsDocument
{
    public string Version { get; init; } = string.Empty;

    public string Algorithm { get; init; } = string.Empty;

    public string Salt { get; init; } = string.Empty;

    public string Nonce { get; init; } = string.Empty;

    public string Ciphertext { get; init; } = string.Empty;
}
