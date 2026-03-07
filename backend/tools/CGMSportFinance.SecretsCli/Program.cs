using CGMSportFinance.Secrets;

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

var command = args[0].Trim().ToLowerInvariant();

try
{
    return command switch
    {
        "encrypt" => Encrypt(args),
        "decrypt" => Decrypt(args),
        _ => UnknownCommand(command),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int Encrypt(string[] args)
{
    var inputPath = args[1];
    var outputPath = args.Length >= 3 ? args[2] : "appsettings.secrets.enc.json";

    var plainJson = File.ReadAllText(inputPath);
    var encryptedJson = EncryptedSecretsSerializer.EncryptJson(plainJson);

    File.WriteAllText(outputPath, $"{encryptedJson}{Environment.NewLine}");
    Console.WriteLine($"Encrypted secrets written to '{Path.GetFullPath(outputPath)}'.");
    return 0;
}

static int Decrypt(string[] args)
{
    var inputPath = args[1];
    var plainJson = EncryptedSecretsSerializer.DecryptJson(File.ReadAllText(inputPath));

    if (args.Length >= 3)
    {
        var outputPath = args[2];
        File.WriteAllText(outputPath, $"{plainJson}{Environment.NewLine}");
        Console.WriteLine($"Decrypted secrets written to '{Path.GetFullPath(outputPath)}'.");
        return 0;
    }

    Console.WriteLine(plainJson);
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/CGMSportFinance.SecretsCli -- encrypt <input-json> [output-json]");
    Console.WriteLine("  dotnet run --project tools/CGMSportFinance.SecretsCli -- decrypt <input-json> [output-json]");
}
