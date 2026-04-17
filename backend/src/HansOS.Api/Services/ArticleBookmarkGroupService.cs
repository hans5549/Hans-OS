using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.ArticleCollection;
using HansOS.Api.Services.Internal;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>文章收藏群組 CRUD。</summary>
public class ArticleBookmarkGroupService(
    ApplicationDbContext db,
    ILogger<ArticleBookmarkGroupService> logger) : IArticleBookmarkGroupService
{
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
        var groupName = ArticleBookmarkMapper.NormalizeRequired(request.Name, "群組名稱");
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
        var groupName = ArticleBookmarkMapper.NormalizeRequired(request.Name, "群組名稱");

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

    private async Task<ArticleBookmarkGroup> GetGroupForUserAsync(
        string userId,
        Guid groupId,
        CancellationToken ct) =>
        await db.ArticleBookmarkGroups
            .FirstOrDefaultAsync(group => group.Id == groupId && group.UserId == userId, ct)
            ?? throw new KeyNotFoundException("文章收藏群組不存在");

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

    private async Task SaveGroupChangesAsync(string groupName, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ArticleBookmarkMapper.IsGroupDuplicateConstraint(ex))
        {
            throw new ArgumentException($"群組名稱「{groupName}」已存在");
        }
    }
}
