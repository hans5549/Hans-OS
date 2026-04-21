using HansOS.Api.Data.Entities;
using HansOS.Api.Models.ArticleCollection;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services.Internal;

/// <summary>
/// ArticleBookmark / ArticleBookmarkGroup 共用的純靜態正規化與映射工具。
/// 不持有 DbContext；任何需要 DB 查詢的邏輯請留在 Service 中。
/// </summary>
internal static class ArticleBookmarkMapper
{
    public const int MaxTags = 20;
    public const int MaxTagLength = 50;
    public const int MaxPageSize = 100;

    public static ArticleBookmarkSourceType ParseSourceType(string? sourceType)
    {
        if (!Enum.TryParse<ArticleBookmarkSourceType>(sourceType, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException("無效的收藏來源類型");
        }

        return parsed;
    }

    public static string NormalizeRequired(string? value, string displayName)
    {
        var normalized = value?.Trim();
        return !string.IsNullOrWhiteSpace(normalized)
            ? normalized
            : throw new ArgumentException($"請輸入{displayName}");
    }

    public static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string[] NormalizeTags(string[]? tags)
    {
        if (tags is null || tags.Length == 0)
        {
            return [];
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            var value = tag?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Length > MaxTagLength)
            {
                throw new ArgumentException($"單一標籤不可超過 {MaxTagLength} 個字元");
            }

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        if (normalized.Count > MaxTags)
        {
            throw new ArgumentException($"標籤數量不可超過 {MaxTags} 個");
        }

        return normalized.ToArray();
    }

    public static NormalizedBookmarkInput NormalizeExternalUrlBookmark(
        ArticleBookmarkSourceType sourceType,
        string? urlRaw,
        string title,
        string? customTitle,
        string? excerptSnapshot,
        string? coverImageUrl,
        string? note,
        Guid? groupId,
        string[] tags)
    {
        var url = NormalizeRequired(urlRaw, "文章連結");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("請輸入有效的 http/https 文章連結");
        }

        return new NormalizedBookmarkInput(
            sourceType,
            null,
            uri.ToString(),
            title,
            customTitle,
            excerptSnapshot,
            coverImageUrl,
            uri.Host.ToLowerInvariant(),
            note,
            groupId,
            tags);
    }

    public static ArticleBookmarkResponse MapBookmark(ArticleBookmark bookmark)
    {
        return new ArticleBookmarkResponse(
            bookmark.Id,
            bookmark.SourceType.ToString(),
            bookmark.SourceId,
            bookmark.Url,
            bookmark.Title,
            bookmark.CustomTitle,
            bookmark.ExcerptSnapshot,
            bookmark.CoverImageUrl,
            bookmark.Domain,
            bookmark.Note,
            bookmark.GroupId,
            bookmark.Group?.Name,
            bookmark.Tags,
            bookmark.IsPinned,
            bookmark.IsRead,
            bookmark.CreatedAt,
            bookmark.UpdatedAt,
            bookmark.LastOpenedAt);
    }

    public static bool IsBookmarkDuplicateConstraint(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ArticleBookmarks_UserId_Url", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_ArticleBookmarks_UserId_SourceId", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsGroupDuplicateConstraint(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ArticleBookmarkGroups_UserId_Name", StringComparison.OrdinalIgnoreCase);
    }

    public sealed record NormalizedBookmarkInput(
        ArticleBookmarkSourceType SourceType,
        string? SourceId,
        string? Url,
        string Title,
        string? CustomTitle,
        string? ExcerptSnapshot,
        string? CoverImageUrl,
        string? Domain,
        string? Note,
        Guid? GroupId,
        string[] Tags);
}
