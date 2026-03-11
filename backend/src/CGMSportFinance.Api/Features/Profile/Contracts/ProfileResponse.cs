namespace CGMSportFinance.Api.Features.Profile.Contracts;

public sealed record ProfileResponse(
    ProfileBasicResponse Basic,
    ProfileHeaderResponse Header,
    ProfileNotificationsResponse Notifications,
    ProfileSecurityResponse Security);
