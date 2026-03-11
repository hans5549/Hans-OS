namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record UpdateProfileNotificationsRequest(
    bool NotifyAccountPassword,
    bool NotifySystemMessage,
    bool NotifyTodoTask);
