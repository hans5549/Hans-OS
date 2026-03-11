using System.ComponentModel.DataAnnotations;

namespace CGMSportFinance.Api.Infrastructure.Identity;

public sealed class UserProfile
{
    [MaxLength(2000)]
    public string Introduction { get; set; } = string.Empty;

    public bool NotifyAccountPassword { get; set; } = true;

    public bool NotifySystemMessage { get; set; } = true;

    public bool NotifyTodoTask { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;

    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
}
