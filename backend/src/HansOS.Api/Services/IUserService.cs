using HansOS.Api.Models.Users;

namespace HansOS.Api.Services;

public interface IUserService
{
    Task<UserInfoResponse> GetUserInfoAsync(string userId, CancellationToken ct = default);
}
