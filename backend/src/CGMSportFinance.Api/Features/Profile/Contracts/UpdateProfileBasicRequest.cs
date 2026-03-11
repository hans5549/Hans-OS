using System.ComponentModel.DataAnnotations;

namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed class UpdateProfileBasicRequest
{
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string Introduction { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    [MaxLength(120)]
    public string RealName { get; init; } = string.Empty;
}
