using System.ComponentModel.DataAnnotations;

namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed class ChangePasswordRequest : IValidatableObject
{
    [MinLength(6)]
    public string ConfirmPassword { get; init; } = string.Empty;

    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;

    [MinLength(6)]
    public string OldPassword { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult("ConfirmPassword must match NewPassword.", [nameof(ConfirmPassword)]);
        }
    }
}
