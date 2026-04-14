using HansOS.Api.Models.ArticleCollection;

namespace HansOS.Api.Services;

public interface IArticleCollectionService
{
    Task<ArticleBookmarkListResponse> GetBookmarksAsync(
        string userId,
        ArticleBookmarkQueryRequest request,
        CancellationToken ct = default);

    Task<ArticleBookmarkResponse> CreateBookmarkAsync(
        string userId,
        CreateArticleBookmarkRequest request,
        CancellationToken ct = default);

    Task<ArticleBookmarkResponse> UpdateBookmarkAsync(
        string userId,
        Guid bookmarkId,
        UpdateArticleBookmarkRequest request,
        CancellationToken ct = default);

    Task<ArticleBookmarkResponse> PatchBookmarkStateAsync(
        string userId,
        Guid bookmarkId,
        PatchArticleBookmarkStateRequest request,
        CancellationToken ct = default);

    Task DeleteBookmarkAsync(
        string userId,
        Guid bookmarkId,
        CancellationToken ct = default);

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
