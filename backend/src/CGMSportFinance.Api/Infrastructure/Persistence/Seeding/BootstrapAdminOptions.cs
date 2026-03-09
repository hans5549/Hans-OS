namespace CGMSportFinance.Api.Infrastructure.Persistence.Seeding;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Avatar { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string HomePath { get; init; } = "/workspace";
    public string Password { get; init; } = string.Empty;
    public string RealName { get; init; } = "Administrator";
    public string Username { get; init; } = string.Empty;

    public bool IsConfigured()
    {
        return
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password);
    }

    public bool IsEmpty()
    {
        return
            string.IsNullOrWhiteSpace(Username) &&
            string.IsNullOrWhiteSpace(Email) &&
            string.IsNullOrWhiteSpace(Password);
    }
}
