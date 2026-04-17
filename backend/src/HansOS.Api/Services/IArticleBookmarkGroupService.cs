using HansOS.Api.Models.ArticleCollection;

namespace HansOS.Api.Services;

public interface IArticleBookmarkGroupService
{
    Task<List<ArticleBookmarkGroupResponse>> GetGroupsAsync(
        string userId,
        CancellationToken ct = default);

    Task<ArticleBookmarkGroupResponse> CreateGroupAsync(
        string userId,
        CreateArticleBookmarkGroupRequest request,
        CancellationToken ct = default);

    Task<ArticleBookmarkGroupResponse> UpdateGroupAsync(
        string userId,
        Guid groupId,
        UpdateArticleBookmarkGroupRequest request,
        CancellationToken ct = default);

    Task DeleteGroupAsync(
        string userId,
        Guid groupId,
        CancellationToken ct = default);
}
