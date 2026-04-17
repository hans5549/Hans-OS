using HansOS.Api.Models.ArticleCollection;

namespace HansOS.Api.Services;

public interface IArticleBookmarkService
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
}
