using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.ArticleCollection;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class ArticleCollectionService(
    ApplicationDbContext db,
    ILogger<ArticleCollectionService> logger) : IArticleCollectionService
{
    private const int MaxTags = 20;
    private const int MaxTagLength = 50;
    private const int MaxPageSize = 100;

    public async Task<ArticleBookmarkListResponse> GetBookmarksAsync(
        string userId,
        ArticleBookmarkQueryRequest request,
        CancellationToken ct = default)
    {
        var query = db.ArticleBookmarks
            .AsNoTracking()
            .Include(bookmark => bookmark.Group)
            .Where(bookmark => bookmark.UserId == userId);

        if (request.GroupId.HasValue)
        {
            query = query.Where(bookmark => bookmark.GroupId == request.GroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceType))
        {
            var sourceType = ParseSourceType(request.SourceType);
            query = query.Where(bookmark => bookmark.SourceType == sourceType);
        }

        if (request.IsPinned.HasValue)
        {
            query = query.Where(bookmark => bookmark.IsPinned == request.IsPinned.Value);
        }

        if (request.IsRead.HasValue)
        {
            query = query.Where(bookmark => bookmark.IsRead == request.IsRead.Value);
        }

        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(bookmark =>
                bookmark.Title.Contains(keyword)
                || (bookmark.CustomTitle != null && bookmark.CustomTitle.Contains(keyword))
                || (bookmark.Url != null && bookmark.Url.Contains(keyword))
                || (bookmark.SourceId != null && bookmark.SourceId.Contains(keyword))
                || (bookmark.Note != null && bookmark.Note.Contains(keyword))
                || (bookmark.Domain != null && bookmark.Domain.Contains(keyword)));
        }

        query = query
            .OrderByDescending(bookmark => bookmark.IsPinned)
            .ThenByDescending(bookmark => bookmark.UpdatedAt)
            .ThenByDescending(bookmark => bookmark.CreatedAt);

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize switch
        {
            <= 0 => 20,
            > MaxPageSize => MaxPageSize,
            _ => request.PageSize,
        };

        var total = await query.CountAsync(ct);
        var bookmarks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ArticleBookmarkListResponse(
            bookmarks.Select(MapBookmark).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<ArticleBookmarkResponse> CreateBookmarkAsync(
        string userId,
        CreateArticleBookmarkRequest request,
        CancellationToken ct = default)
    {
        var normalized = await NormalizeBookmarkInputAsync(
            userId,
            request.SourceType,
            request.SourceId,
            request.Url,
            request.Title,
            request.CustomTitle,
            request.ExcerptSnapshot,
            request.CoverImageUrl,
            request.Note,
            request.GroupId,
            request.Tags,
            ct);

        await EnsureDuplicateDoesNotExistAsync(
            userId,
            null,
            normalized.SourceType,
            normalized.SourceId,
            normalized.Url,
            ct);

        var now = DateTime.UtcNow;
        var bookmark = new ArticleBookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceType = normalized.SourceType,
            SourceId = normalized.SourceId,
            Url = normalized.Url,
            Title = normalized.Title,
            CustomTitle = normalized.CustomTitle,
            ExcerptSnapshot = normalized.ExcerptSnapshot,
            CoverImageUrl = normalized.CoverImageUrl,
            Domain = normalized.Domain,
            Note = normalized.Note,
            GroupId = normalized.GroupId,
            Tags = normalized.Tags,
            IsPinned = request.IsPinned,
            IsRead = request.IsRead,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.ArticleBookmarks.Add(bookmark);
        await SaveBookmarkChangesAsync(normalized.SourceType, ct);

        logger.LogInformation("文章收藏已建立: {BookmarkId}, UserId={UserId}", bookmark.Id, userId);

        return await GetBookmarkResponseAsync(userId, bookmark.Id, ct);
    }

    public async Task<ArticleBookmarkResponse> UpdateBookmarkAsync(
        string userId,
        Guid bookmarkId,
        UpdateArticleBookmarkRequest request,
        CancellationToken ct = default)
    {
        var bookmark = await GetBookmarkForUserAsync(userId, bookmarkId, ct);

        var normalized = await NormalizeBookmarkInputAsync(
            userId,
            request.SourceType,
            request.SourceId,
            request.Url,
            request.Title,
            request.CustomTitle,
            request.ExcerptSnapshot,
            request.CoverImageUrl,
            request.Note,
            request.GroupId,
            request.Tags,
            ct);

        await EnsureDuplicateDoesNotExistAsync(
            userId,
            bookmarkId,
            normalized.SourceType,
            normalized.SourceId,
            normalized.Url,
            ct);

        bookmark.SourceType = normalized.SourceType;
        bookmark.SourceId = normalized.SourceId;
        bookmark.Url = normalized.Url;
        bookmark.Title = normalized.Title;
        bookmark.CustomTitle = normalized.CustomTitle;
        bookmark.ExcerptSnapshot = normalized.ExcerptSnapshot;
        bookmark.CoverImageUrl = normalized.CoverImageUrl;
        bookmark.Domain = normalized.Domain;
        bookmark.Note = normalized.Note;
        bookmark.GroupId = normalized.GroupId;
        bookmark.Tags = normalized.Tags;
        bookmark.IsPinned = request.IsPinned;
        bookmark.IsRead = request.IsRead;
        bookmark.UpdatedAt = DateTime.UtcNow;

        await SaveBookmarkChangesAsync(normalized.SourceType, ct);

        logger.LogInformation("文章收藏已更新: {BookmarkId}, UserId={UserId}", bookmarkId, userId);

        return await GetBookmarkResponseAsync(userId, bookmarkId, ct);
    }

    public async Task<ArticleBookmarkResponse> PatchBookmarkStateAsync(
        string userId,
        Guid bookmarkId,
        PatchArticleBookmarkStateRequest request,
        CancellationToken ct = default)
    {
        var bookmark = await GetBookmarkForUserAsync(userId, bookmarkId, ct);

        if (!request.IsPinned.HasValue && !request.IsRead.HasValue)
        {
            throw new ArgumentException("至少要更新一個狀態欄位");
        }

        if (request.IsPinned.HasValue)
        {
            bookmark.IsPinned = request.IsPinned.Value;
        }

        if (request.IsRead.HasValue)
        {
            bookmark.IsRead = request.IsRead.Value;
        }

        bookmark.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("文章收藏狀態已更新: {BookmarkId}, UserId={UserId}", bookmarkId, userId);

        return await GetBookmarkResponseAsync(userId, bookmarkId, ct);
    }

    public async Task DeleteBookmarkAsync(
        string userId,
        Guid bookmarkId,
        CancellationToken ct = default)
    {
        var bookmark = await GetBookmarkForUserAsync(userId, bookmarkId, ct);

        db.ArticleBookmarks.Remove(bookmark);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("文章收藏已刪除: {BookmarkId}, UserId={UserId}", bookmarkId, userId);
    }

    public async Task<List<ArticleBookmarkGroupResponse>> GetGroupsAsync(
        string userId,
        CancellationToken ct = default)
    {
        var groups = await db.ArticleBookmarkGroups
            .AsNoTracking()
            .Where(group => group.UserId == userId)
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.Name)
            .ToListAsync(ct);

        var bookmarkCounts = await db.ArticleBookmarks
            .AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId && bookmark.GroupId != null)
            .GroupBy(bookmark => bookmark.GroupId!.Value)
            .Select(group => new
            {
                GroupId = group.Key,
                Count = group.Count(),
            })
            .ToDictionaryAsync(item => item.GroupId, item => item.Count, ct);

        return groups
            .Select(group => new ArticleBookmarkGroupResponse(
                group.Id,
                group.Name,
                group.SortOrder,
                bookmarkCounts.GetValueOrDefault(group.Id)))
            .ToList();
    }

    public async Task<ArticleBookmarkGroupResponse> CreateGroupAsync(
        string userId,
        CreateArticleBookmarkGroupRequest request,
        CancellationToken ct = default)
    {
        var groupName = NormalizeRequired(request.Name, "群組名稱");
        await EnsureGroupNameAvailableAsync(userId, groupName, null, ct);

        var now = DateTime.UtcNow;
        var group = new ArticleBookmarkGroup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = groupName,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.ArticleBookmarkGroups.Add(group);
        await SaveGroupChangesAsync(groupName, ct);

        logger.LogInformation("文章收藏群組已建立: {GroupId}, UserId={UserId}", group.Id, userId);

        return new ArticleBookmarkGroupResponse(group.Id, group.Name, group.SortOrder, 0);
    }

    public async Task<ArticleBookmarkGroupResponse> UpdateGroupAsync(
        string userId,
        Guid groupId,
        UpdateArticleBookmarkGroupRequest request,
        CancellationToken ct = default)
    {
        var group = await GetGroupForUserAsync(userId, groupId, ct);
        var groupName = NormalizeRequired(request.Name, "群組名稱");

        await EnsureGroupNameAvailableAsync(userId, groupName, groupId, ct);

        group.Name = groupName;
        group.SortOrder = request.SortOrder;
        group.UpdatedAt = DateTime.UtcNow;

        await SaveGroupChangesAsync(groupName, ct);

        logger.LogInformation("文章收藏群組已更新: {GroupId}, UserId={UserId}", groupId, userId);

        var bookmarkCount = await db.ArticleBookmarks
            .AsNoTracking()
            .CountAsync(bookmark => bookmark.UserId == userId && bookmark.GroupId == groupId, ct);

        return new ArticleBookmarkGroupResponse(group.Id, group.Name, group.SortOrder, bookmarkCount);
    }

    public async Task DeleteGroupAsync(
        string userId,
        Guid groupId,
        CancellationToken ct = default)
    {
        var group = await GetGroupForUserAsync(userId, groupId, ct);
        var bookmarks = await db.ArticleBookmarks
            .Where(bookmark => bookmark.UserId == userId && bookmark.GroupId == groupId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var bookmark in bookmarks)
        {
            bookmark.GroupId = null;
            bookmark.UpdatedAt = now;
        }

        db.ArticleBookmarkGroups.Remove(group);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("文章收藏群組已刪除: {GroupId}, UserId={UserId}", groupId, userId);
    }

    private async Task<ArticleBookmark> GetBookmarkForUserAsync(
        string userId,
        Guid bookmarkId,
        CancellationToken ct)
    {
        return await db.ArticleBookmarks
            .FirstOrDefaultAsync(bookmark => bookmark.Id == bookmarkId && bookmark.UserId == userId, ct)
            ?? throw new KeyNotFoundException("文章收藏不存在");
    }

    private async Task<ArticleBookmarkGroup> GetGroupForUserAsync(
        string userId,
        Guid groupId,
        CancellationToken ct)
    {
        return await db.ArticleBookmarkGroups
            .FirstOrDefaultAsync(group => group.Id == groupId && group.UserId == userId, ct)
            ?? throw new KeyNotFoundException("文章收藏群組不存在");
    }

    private async Task<ArticleBookmarkResponse> GetBookmarkResponseAsync(
        string userId,
        Guid bookmarkId,
        CancellationToken ct)
    {
        var bookmark = await db.ArticleBookmarks
            .AsNoTracking()
            .Include(item => item.Group)
            .FirstOrDefaultAsync(item => item.Id == bookmarkId && item.UserId == userId, ct)
            ?? throw new KeyNotFoundException("文章收藏不存在");

        return MapBookmark(bookmark);
    }

    private async Task EnsureGroupNameAvailableAsync(
        string userId,
        string name,
        Guid? existingGroupId,
        CancellationToken ct)
    {
        var duplicateExists = await db.ArticleBookmarkGroups
            .AnyAsync(
                group =>
                    group.UserId == userId
                    && group.Name == name
                    && (!existingGroupId.HasValue || group.Id != existingGroupId.Value),
                ct);

        if (duplicateExists)
        {
            throw new ArgumentException($"群組名稱「{name}」已存在");
        }
    }

    private async Task EnsureDuplicateDoesNotExistAsync(
        string userId,
        Guid? existingBookmarkId,
        ArticleBookmarkSourceType sourceType,
        string? sourceId,
        string? url,
        CancellationToken ct)
    {
        var duplicateExists = sourceType switch
        {
            ArticleBookmarkSourceType.ExternalUrl => await db.ArticleBookmarks
                .AnyAsync(
                    bookmark =>
                        bookmark.UserId == userId
                        && bookmark.SourceType == ArticleBookmarkSourceType.ExternalUrl
                        && bookmark.Url == url
                        && (!existingBookmarkId.HasValue || bookmark.Id != existingBookmarkId.Value),
                    ct),
            ArticleBookmarkSourceType.InternalArticle => await db.ArticleBookmarks
                .AnyAsync(
                    bookmark =>
                        bookmark.UserId == userId
                        && bookmark.SourceType == ArticleBookmarkSourceType.InternalArticle
                        && bookmark.SourceId == sourceId
                        && (!existingBookmarkId.HasValue || bookmark.Id != existingBookmarkId.Value),
                    ct),
            _ => false,
        };

        if (duplicateExists)
        {
            throw sourceType switch
            {
                ArticleBookmarkSourceType.ExternalUrl => new ArgumentException("此外部文章已收藏"),
                ArticleBookmarkSourceType.InternalArticle => new ArgumentException("此內建文章已收藏"),
                _ => new ArgumentException("收藏資料重複"),
            };
        }
    }

    private async Task<NormalizedBookmarkInput> NormalizeBookmarkInputAsync(
        string userId,
        string sourceTypeRaw,
        string? sourceIdRaw,
        string? urlRaw,
        string titleRaw,
        string? customTitleRaw,
        string? excerptRaw,
        string? coverImageUrlRaw,
        string? noteRaw,
        Guid? groupId,
        string[]? tagsRaw,
        CancellationToken ct)
    {
        var sourceType = ParseSourceType(sourceTypeRaw);
        var title = NormalizeRequired(titleRaw, "標題");
        var customTitle = NormalizeOptional(customTitleRaw);
        var excerptSnapshot = NormalizeOptional(excerptRaw);
        var coverImageUrl = NormalizeOptional(coverImageUrlRaw);
        var note = NormalizeOptional(noteRaw);
        var tags = NormalizeTags(tagsRaw);

        if (groupId.HasValue)
        {
            _ = await GetGroupForUserAsync(userId, groupId.Value, ct);
        }

        return sourceType switch
        {
            ArticleBookmarkSourceType.ExternalUrl => NormalizeExternalUrlBookmark(
                sourceType,
                urlRaw,
                title,
                customTitle,
                excerptSnapshot,
                coverImageUrl,
                note,
                groupId,
                tags),
            ArticleBookmarkSourceType.InternalArticle
                => throw new ArgumentException("內建文章收藏尚未完成文章系統整合，請先使用外部文章連結"),
            _ => throw new ArgumentException("無效的收藏來源類型"),
        };
    }

    private static NormalizedBookmarkInput NormalizeExternalUrlBookmark(
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

    private static NormalizedBookmarkInput NormalizeInternalArticleBookmark(
        ArticleBookmarkSourceType sourceType,
        string? sourceIdRaw,
        string title,
        string? customTitle,
        string? excerptSnapshot,
        string? coverImageUrl,
        string? note,
        Guid? groupId,
        string[] tags)
    {
        var sourceId = NormalizeRequired(sourceIdRaw, "文章來源 ID");

        return new NormalizedBookmarkInput(
            sourceType,
            sourceId,
            null,
            title,
            customTitle,
            excerptSnapshot,
            coverImageUrl,
            null,
            note,
            groupId,
            tags);
    }

    private static ArticleBookmarkSourceType ParseSourceType(string? sourceType)
    {
        if (!Enum.TryParse<ArticleBookmarkSourceType>(sourceType, ignoreCase: true, out var parsed))
        {
            throw new ArgumentException("無效的收藏來源類型");
        }

        return parsed;
    }

    private static string NormalizeRequired(string? value, string displayName)
    {
        var normalized = value?.Trim();
        return !string.IsNullOrWhiteSpace(normalized)
            ? normalized
            : throw new ArgumentException($"請輸入{displayName}");
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string[] NormalizeTags(string[]? tags)
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

    private async Task SaveBookmarkChangesAsync(
        ArticleBookmarkSourceType sourceType,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateConstraint(ex))
        {
            throw sourceType switch
            {
                ArticleBookmarkSourceType.ExternalUrl => new ArgumentException("此外部文章已收藏"),
                ArticleBookmarkSourceType.InternalArticle => new ArgumentException("此內建文章已收藏"),
                _ => new ArgumentException("收藏資料重複"),
            };
        }
    }

    private async Task SaveGroupChangesAsync(
        string groupName,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsGroupDuplicateConstraint(ex))
        {
            throw new ArgumentException($"群組名稱「{groupName}」已存在");
        }
    }

    private static ArticleBookmarkResponse MapBookmark(ArticleBookmark bookmark)
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

    private static bool IsDuplicateConstraint(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ArticleBookmarks_UserId_Url", StringComparison.OrdinalIgnoreCase)
            || message.Contains("IX_ArticleBookmarks_UserId_SourceId", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGroupDuplicateConstraint(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_ArticleBookmarkGroups_UserId_Name", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record NormalizedBookmarkInput(
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
