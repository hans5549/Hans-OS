using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.ArticleCollection;
using HansOS.Api.Services.Internal;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>文章書籤 CRUD 與狀態更新。共用正規化邏輯見 <see cref="ArticleBookmarkMapper"/>。</summary>
public class ArticleBookmarkService(
    ApplicationDbContext db,
    ILogger<ArticleBookmarkService> logger) : IArticleBookmarkService
{
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
            var sourceType = ArticleBookmarkMapper.ParseSourceType(request.SourceType);
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
            > ArticleBookmarkMapper.MaxPageSize => ArticleBookmarkMapper.MaxPageSize,
            _ => request.PageSize,
        };

        var total = await query.CountAsync(ct);
        var bookmarks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ArticleBookmarkListResponse(
            bookmarks.Select(ArticleBookmarkMapper.MapBookmark).ToList(),
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

    private async Task<ArticleBookmark> GetBookmarkForUserAsync(
        string userId,
        Guid bookmarkId,
        CancellationToken ct) =>
        await db.ArticleBookmarks
            .FirstOrDefaultAsync(bookmark => bookmark.Id == bookmarkId && bookmark.UserId == userId, ct)
            ?? throw new KeyNotFoundException("文章收藏不存在");

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

        return ArticleBookmarkMapper.MapBookmark(bookmark);
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

    private async Task<ArticleBookmarkMapper.NormalizedBookmarkInput> NormalizeBookmarkInputAsync(
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
        var sourceType = ArticleBookmarkMapper.ParseSourceType(sourceTypeRaw);
        var title = ArticleBookmarkMapper.NormalizeRequired(titleRaw, "標題");
        var customTitle = ArticleBookmarkMapper.NormalizeOptional(customTitleRaw);
        var excerptSnapshot = ArticleBookmarkMapper.NormalizeOptional(excerptRaw);
        var coverImageUrl = ArticleBookmarkMapper.NormalizeOptional(coverImageUrlRaw);
        var note = ArticleBookmarkMapper.NormalizeOptional(noteRaw);
        var tags = ArticleBookmarkMapper.NormalizeTags(tagsRaw);

        if (groupId.HasValue)
        {
            await EnsureGroupOwnedByUserAsync(userId, groupId.Value, ct);
        }

        return sourceType switch
        {
            ArticleBookmarkSourceType.ExternalUrl => ArticleBookmarkMapper.NormalizeExternalUrlBookmark(
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

    private async Task EnsureGroupOwnedByUserAsync(string userId, Guid groupId, CancellationToken ct)
    {
        var exists = await db.ArticleBookmarkGroups
            .AsNoTracking()
            .AnyAsync(group => group.Id == groupId && group.UserId == userId, ct);

        if (!exists)
        {
            throw new KeyNotFoundException("文章收藏群組不存在");
        }
    }

    private async Task SaveBookmarkChangesAsync(
        ArticleBookmarkSourceType sourceType,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ArticleBookmarkMapper.IsBookmarkDuplicateConstraint(ex))
        {
            throw sourceType switch
            {
                ArticleBookmarkSourceType.ExternalUrl => new ArgumentException("此外部文章已收藏"),
                ArticleBookmarkSourceType.InternalArticle => new ArgumentException("此內建文章已收藏"),
                _ => new ArgumentException("收藏資料重複"),
            };
        }
    }
}
