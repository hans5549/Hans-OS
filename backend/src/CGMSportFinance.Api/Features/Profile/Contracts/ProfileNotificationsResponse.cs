namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record ProfileNotificationsResponse(
    bool NotifyAccountPassword,
    bool NotifySystemMessage,
    bool NotifyTodoTask);
