using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.ArticleCollection;

public class ArticleBookmarkQueryRequest
{
    public string? Keyword { get; init; }
    public Guid? GroupId { get; init; }
    public string? SourceType { get; init; }
    public bool? IsPinned { get; init; }
    public bool? IsRead { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record CreateArticleBookmarkRequest(
    [Required] string SourceType,
    [StringLength(200)] string? SourceId,
    [StringLength(2048)] string? Url,
    [Required][StringLength(300)] string Title,
    [StringLength(300)] string? CustomTitle = null,
    [StringLength(1000)] string? ExcerptSnapshot = null,
    [StringLength(2048)] string? CoverImageUrl = null,
    [StringLength(1000)] string? Note = null,
    Guid? GroupId = null,
    string[]? Tags = null,
    bool IsPinned = false,
    bool IsRead = false);

public record UpdateArticleBookmarkRequest(
    [Required] string SourceType,
    [StringLength(200)] string? SourceId,
    [StringLength(2048)] string? Url,
    [Required][StringLength(300)] string Title,
    [StringLength(300)] string? CustomTitle = null,
    [StringLength(1000)] string? ExcerptSnapshot = null,
    [StringLength(2048)] string? CoverImageUrl = null,
    [StringLength(1000)] string? Note = null,
    Guid? GroupId = null,
    string[]? Tags = null,
    bool IsPinned = false,
    bool IsRead = false);

public record PatchArticleBookmarkStateRequest(
    bool? IsPinned,
    bool? IsRead);

public record ArticleBookmarkResponse(
    Guid Id,
    string SourceType,
    string? SourceId,
    string? Url,
    string Title,
    string? CustomTitle,
    string? ExcerptSnapshot,
    string? CoverImageUrl,
    string? Domain,
    string? Note,
    Guid? GroupId,
    string? GroupName,
    string[] Tags,
    bool IsPinned,
    bool IsRead,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastOpenedAt);

public record ArticleBookmarkListResponse(
    IReadOnlyList<ArticleBookmarkResponse> Items,
    int Total,
    int Page,
    int PageSize);

public record CreateArticleBookmarkGroupRequest(
    [Required][StringLength(100)] string Name,
    int SortOrder = 0);

public record UpdateArticleBookmarkGroupRequest(
    [Required][StringLength(100)] string Name,
    int SortOrder = 0);

public record ArticleBookmarkGroupResponse(
    Guid Id,
    string Name,
    int SortOrder,
    int BookmarkCount);
