using HansOS.Api.Models.Users;

namespace HansOS.Api.Services;

public interface IUserService
{
    Task<UserInfoResponse> GetUserInfoAsync(string userId, CancellationToken ct = default);
    Task UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
}
