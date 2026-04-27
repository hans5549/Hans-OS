using HansOS.Api.Common;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using HansOS.Api.Services.Internal;
using Microsoft.EntityFrameworkCore;
using static HansOS.Api.Services.Internal.TodoItemMapper;
using static HansOS.Api.Services.Internal.TodoItemQueries;
using static HansOS.Api.Services.Internal.TodoItemStatusRules;

namespace HansOS.Api.Services;

/// <summary>待辦事項服務</summary>
public class TodoItemService(ApplicationDbContext db) : ITodoItemService
{
    /// <inheritdoc />
    public async Task<PagedItemsResponse> GetItemsAsync(PagedItemsQuery query, CancellationToken ct = default)
    {
        var q = ActiveItems(db, query.UserId);

        q = ApplyViewFilter(q, query.View, query.ProjectId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ParseStatus(query.Status);
            q = q.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = ParsePriority(query.Priority);
            q = q.Where(i => i.Priority == priority);
        }

        if (query.TopLevelOnly)
            q = q.Where(i => i.ParentId == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(i => i.Title.Contains(query.Search));

        if (query.IncludeChildren)
            q = IncludeActiveChildren(q, query.UserId);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(i => i.Order)
            .ThenBy(i => i.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedItemsResponse(
            items.Select(ToItemResponseFromEntity).ToList(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    /// <inheritdoc />
    public async Task<ItemDetailResponse?> GetItemDetailAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await IncludeActiveChildren(
                db.TodoItems
                    .AsNoTracking()
                    .Include(i => i.Project)
                    .Include(i => i.Category)
                    .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag),
                userId)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId && i.DeletedAt == null, ct);

        return item is null ? null : ToItemDetailResponse(item);
    }

    /// <inheritdoc />
    public async Task<TodoCountsResponse> GetCountsAsync(string userId, CancellationToken ct = default)
    {
        var counts = await db.TodoItems
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Status != TodoStatus.Done && i.DeletedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inbox = g.Count(i => i.ProjectId == null),
                Today = g.Count(i => i.DueDate == Today),
                Upcoming = g.Count(i => i.DueDate > Today && i.DueDate <= UpcomingEnd),
                All = g.Count(),
            })
            .FirstOrDefaultAsync(ct);

        return counts is null
            ? new TodoCountsResponse(0, 0, 0, 0)
            : new TodoCountsResponse(counts.Inbox, counts.Today, counts.Upcoming, counts.All);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> CreateItemAsync(
        string userId, CreateItemRequest request, CancellationToken ct = default)
    {
        var projectId = await ValidateProjectOwnershipAsync(db, userId, request.ProjectId, ct);
        var parent = await ValidateParentAsync(userId, request.ParentId, ct);
        var categoryId = await ValidateCategoryOwnershipAsync(db, userId, request.CategoryId, ct);
        if (parent is not null && projectId is null)
            projectId = parent.ProjectId;
        if (parent is not null && categoryId is null)
            categoryId = parent.CategoryId;

        var maxOrder = await db.TodoItems
            .Where(i => i.UserId == userId && i.ProjectId == projectId && i.ParentId == request.ParentId)
            .Select(i => (int?)i.Order)
            .MaxAsync(ct) ?? -1;

        var utcNow = DateTime.UtcNow;
        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            ParentId = request.ParentId,
            CategoryId = categoryId,
            Title = request.Title,
            Description = request.Description,
            Priority = ParsePriority(request.Priority),
            Difficulty = ParseDifficulty(request.Difficulty),
            Status = TodoStatus.Pending,
            DueDate = request.DueDate,
            ScheduledDate = request.ScheduledDate,
            RecurrencePattern = ParseRecurrencePattern(request.RecurrencePattern),
            RecurrenceInterval = request.RecurrenceInterval,
            Order = maxOrder + 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        db.TodoItems.Add(item);

        await TodoItemTagCommands.AddTagsAsync(db, userId, item.Id, request.TagIds, ct);

        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(db, item.Id, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> UpdateItemAsync(
        string userId, Guid itemId, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(db, userId, itemId, ct);
        if (request.ParentId == itemId)
            throw new ArgumentException("任務不能設定自身為父任務");
        if (request.ParentId is not null)
            await EnsureCanMoveUnderParentAsync(userId, itemId, ct);

        var projectId = await ValidateProjectOwnershipAsync(db, userId, request.ProjectId, ct);
        var parent = await ValidateParentAsync(userId, request.ParentId, ct);
        var categoryId = await ValidateCategoryOwnershipAsync(db, userId, request.CategoryId, ct);
        var utcNow = DateTime.UtcNow;

        if (parent is not null && projectId is null)
            projectId = parent.ProjectId;
        if (parent is not null && categoryId is null)
            categoryId = parent.CategoryId;

        item.Title = request.Title;
        item.Description = request.Description;
        item.Priority = ParsePriority(request.Priority);
        item.Difficulty = ParseDifficulty(request.Difficulty);
        item.DueDate = request.DueDate;
        item.ScheduledDate = request.ScheduledDate;
        item.ProjectId = projectId;
        item.ParentId = request.ParentId;
        item.CategoryId = categoryId;
        item.RecurrencePattern = ParseRecurrencePattern(request.RecurrencePattern);
        item.RecurrenceInterval = request.RecurrenceInterval;
        item.UpdatedAt = utcNow;
        SetItemStatus(item, ParseStatus(request.Status), utcNow);

        await TodoItemTagCommands.ReplaceTagsAsync(db, userId, itemId, request.TagIds, ct);

        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(db, itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> ToggleCompleteAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(db, userId, itemId, ct);
        ToggleCompletedState(item, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(db, itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemDetailResponse> UpdateStatusAsync(
        string userId, Guid itemId, string status, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(db, userId, itemId, ct);
        var utcNow = DateTime.UtcNow;
        var newStatus = ParseStatusFromClient(status);

        SetItemStatus(item, newStatus, utcNow);
        item.UpdatedAt = utcNow;

        if (newStatus == TodoStatus.Done && item.RecurrencePattern != RecurrencePattern.None)
            db.TodoItems.Add(TodoItemRecurrenceFactory.CreateNext(item, utcNow));

        await db.SaveChangesAsync(ct);

        var detail = await GetItemDetailAsync(userId, itemId, ct);
        return detail!;
    }

    /// <inheritdoc />
    public async Task<ItemDetailResponse> ArchiveAsync(string userId, Guid itemId, bool archive, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(db, userId, itemId, ct);
        var utcNow = DateTime.UtcNow;
        var previousArchivedAt = item.ArchivedAt;
        item.ArchivedAt = archive ? utcNow : null;
        item.UpdatedAt = utcNow;
        await ApplyArchiveToChildrenAsync(userId, itemId, archive, utcNow, previousArchivedAt, ct);
        await db.SaveChangesAsync(ct);
        var detail = await GetItemDetailAsync(userId, itemId, ct);
        return detail!;
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(db, userId, itemId, ct);
        var utcNow = DateTime.UtcNow;
        item.DeletedAt = utcNow;
        item.UpdatedAt = utcNow;
        await ApplyDeleteToActiveChildrenAsync(userId, itemId, utcNow, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetTrashAsync(string userId, CancellationToken ct = default)
        => await ToItemResponsesAsync(
            ListItems(db, userId)
                .Where(i => i.DeletedAt != null)
                .OrderByDescending(i => i.DeletedAt),
            ct);

    /// <inheritdoc />
    public async Task RestoreAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        var deletedAt = item.DeletedAt;
        item.DeletedAt = null;
        item.UpdatedAt = DateTime.UtcNow;
        await RestoreCascadeDeletedChildrenAsync(userId, itemId, deletedAt, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task PermanentDeleteAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        var children = await db.TodoItems
            .Where(i => i.ParentId == itemId && i.UserId == userId)
            .ToListAsync(ct);

        db.TodoItems.RemoveRange(children);
        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetTodayAsync(string userId, CancellationToken ct = default)
        => await ToItemResponsesAsync(
            ListItems(db, userId)
                .Where(i => i.DeletedAt == null
                    && (i.DueDate == Today || i.ScheduledDate == Today))
                .OrderBy(i => i.Order).ThenBy(i => i.CreatedAt),
            ct);

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetWeekAsync(string userId, CancellationToken ct = default)
    {
        var weekEnd = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        return await ToItemResponsesAsync(
            ListItems(db, userId)
                .Where(i => i.DeletedAt == null
                    && ((i.DueDate >= Today && i.DueDate <= weekEnd)
                        || (i.ScheduledDate >= Today && i.ScheduledDate <= weekEnd)))
                .OrderBy(i => i.DueDate ?? i.ScheduledDate).ThenBy(i => i.Order),
            ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetMonthAsync(string userId, CancellationToken ct = default)
    {
        var monthStart = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var monthEnd = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1));
        return await ToItemResponsesAsync(
            ListItems(db, userId)
                .Where(i => i.DeletedAt == null
                    && ((i.DueDate >= monthStart && i.DueDate <= monthEnd)
                        || (i.ScheduledDate >= monthStart && i.ScheduledDate <= monthEnd)))
                .OrderBy(i => i.DueDate ?? i.ScheduledDate).ThenBy(i => i.Order),
            ct);
    }

    /// <inheritdoc />
    public async Task<TodoStatsResponse> GetStatsAsync(string userId, CancellationToken ct = default)
    {
        var stats = await db.TodoItems
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.DeletedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(i => i.ArchivedAt == null),
                Pending = g.Count(i => i.Status == TodoStatus.Pending && i.ArchivedAt == null),
                InProgress = g.Count(i => i.Status == TodoStatus.InProgress && i.ArchivedAt == null),
                Completed = g.Count(i => i.Status == TodoStatus.Done && i.ArchivedAt == null),
                Overdue = g.Count(i => i.DueDate < Today && i.Status != TodoStatus.Done && i.ArchivedAt == null),
                Archived = g.Count(i => i.ArchivedAt != null),
            })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new TodoStatsResponse(0, 0, 0, 0, 0, 0)
            : new TodoStatsResponse(stats.Total, stats.Pending, stats.InProgress, stats.Completed, stats.Overdue, stats.Archived);
    }

    /// <inheritdoc />
    public async Task<int> GetReminderCountAsync(string userId, CancellationToken ct = default)
        => await db.TodoItems
            .AsNoTracking()
            .CountAsync(i => i.UserId == userId && i.DeletedAt == null
                && i.Status != TodoStatus.Done
                && (i.DueDate <= Today), ct);

    /// <inheritdoc />
    public async Task<List<ItemResponse>> SearchAsync(string userId, string q, CancellationToken ct = default)
        => await ToItemResponsesAsync(
            ListItems(db, userId)
                .Where(i => i.DeletedAt == null && i.Title.Contains(q))
                .OrderBy(i => i.Order).ThenBy(i => i.CreatedAt),
            ct);

    /// <inheritdoc />
    public async Task BatchUpdateAsync(string userId, List<Guid> ids, string status, CancellationToken ct = default)
    {
        var newStatus = ParseStatusFromClient(status);
        var utcNow = DateTime.UtcNow;
        var items = await db.TodoItems
            .Where(i => ids.Contains(i.Id) && i.UserId == userId)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            SetItemStatus(item, newStatus, utcNow);
            item.UpdatedAt = utcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SortAsync(string userId, List<Guid> orderedIds, CancellationToken ct = default)
    {
        var items = await db.TodoItems
            .Where(i => orderedIds.Contains(i.Id) && i.UserId == userId)
            .ToListAsync(ct);

        var orderMap = orderedIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
        foreach (var item in items.Where(i => orderMap.ContainsKey(i.Id)))
            item.Order = orderMap[item.Id];

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> ReorderChildrenAsync(
        string userId,
        Guid parentId,
        List<Guid> orderedChildIds,
        CancellationToken ct = default)
    {
        _ = await GetUserItemAsync(db, userId, parentId, ct);
        if (orderedChildIds.Count != orderedChildIds.Distinct().Count())
            throw new ArgumentException("子任務排序清單包含重複任務");

        var foreignChildExists = await db.TodoItems
            .AsNoTracking()
            .AnyAsync(i => orderedChildIds.Contains(i.Id) && i.UserId != userId, ct);
        if (foreignChildExists)
            throw new ForbiddenException("無權限存取指定的子任務");

        var children = await db.TodoItems
            .Where(i => i.ParentId == parentId
                && i.UserId == userId
                && i.DeletedAt == null
                && i.ArchivedAt == null)
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .ToListAsync(ct);

        if (!children.Select(i => i.Id).Order().SequenceEqual(orderedChildIds.Order()))
            throw new ArgumentException("子任務排序清單必須完整包含所有直接子任務");

        var orderMap = orderedChildIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);
        foreach (var child in children)
            child.Order = orderMap[child.Id];

        await db.SaveChangesAsync(ct);

        return children
            .OrderBy(i => i.Order)
            .ThenBy(i => i.CreatedAt)
            .Select(ToItemResponseFromEntity)
            .ToList();
    }

    private async Task<TodoItem?> ValidateParentAsync(string userId, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return null;

        var parent = await GetUserItemAsync(db, userId, parentId.Value, ct);
        if (parent.ParentId is not null)
            throw new ArgumentException("目前只支援一層子任務");

        return parent;
    }

    private static IQueryable<TodoItem> IncludeActiveChildren(IQueryable<TodoItem> query, string userId)
        => query
            .Include(i => i.Children
                .Where(c => c.UserId == userId && c.DeletedAt == null && c.ArchivedAt == null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.CreatedAt))
            .ThenInclude(c => c.Project)
            .Include(i => i.Children
                .Where(c => c.UserId == userId && c.DeletedAt == null && c.ArchivedAt == null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.CreatedAt))
            .ThenInclude(c => c.TodoItemTags).ThenInclude(t => t.TodoTag);

    private async Task EnsureCanMoveUnderParentAsync(string userId, Guid itemId, CancellationToken ct)
    {
        var hasActiveChildren = await db.TodoItems
            .AsNoTracking()
            .AnyAsync(i => i.ParentId == itemId
                && i.UserId == userId
                && i.DeletedAt == null
                && i.ArchivedAt == null, ct);
        if (hasActiveChildren)
            throw new ArgumentException("有子任務的任務不能再設定父任務");
    }

    private async Task ApplyArchiveToChildrenAsync(
        string userId,
        Guid parentId,
        bool archive,
        DateTime utcNow,
        DateTime? previousArchivedAt,
        CancellationToken ct)
    {
        var children = await db.TodoItems
            .Where(i => i.ParentId == parentId && i.UserId == userId && i.DeletedAt == null)
            .Where(i => archive ? i.ArchivedAt == null : i.ArchivedAt == previousArchivedAt)
            .ToListAsync(ct);

        foreach (var child in children)
        {
            child.ArchivedAt = archive ? utcNow : null;
            child.UpdatedAt = utcNow;
        }
    }

    private async Task ApplyDeleteToActiveChildrenAsync(
        string userId,
        Guid parentId,
        DateTime utcNow,
        CancellationToken ct)
    {
        var children = await db.TodoItems
            .Where(i => i.ParentId == parentId
                && i.UserId == userId
                && i.DeletedAt == null
                && i.ArchivedAt == null)
            .ToListAsync(ct);

        foreach (var child in children)
        {
            child.DeletedAt = utcNow;
            child.UpdatedAt = utcNow;
        }
    }

    private async Task RestoreCascadeDeletedChildrenAsync(
        string userId,
        Guid parentId,
        DateTime? deletedAt,
        CancellationToken ct)
    {
        if (deletedAt is null) return;

        var children = await db.TodoItems
            .Where(i => i.ParentId == parentId && i.UserId == userId && i.DeletedAt == deletedAt)
            .ToListAsync(ct);

        var utcNow = DateTime.UtcNow;
        foreach (var child in children)
        {
            child.DeletedAt = null;
            child.UpdatedAt = utcNow;
        }
    }
}
