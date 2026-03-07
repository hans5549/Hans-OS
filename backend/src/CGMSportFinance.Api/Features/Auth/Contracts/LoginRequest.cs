using System.ComponentModel.DataAnnotations;

namespace CGMSportFinance.Api.Features.Auth.Contracts;

public sealed class LoginRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Username { get; init; } = string.Empty;
}
