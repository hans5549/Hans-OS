using System.Linq.Expressions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>待辦事項服務</summary>
public class TodoItemService(ApplicationDbContext db) : ITodoItemService
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    private static DateOnly UpcomingEnd => DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    /// <inheritdoc />
    public async Task<PagedItemsResponse> GetItemsAsync(PagedItemsQuery query, CancellationToken ct = default)
    {
        var q = db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == query.UserId && i.DeletedAt == null && i.ArchivedAt == null);

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
        var item = await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.Category)
            .Include(i => i.ChecklistItems)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Include(i => i.Children).ThenInclude(c => c.Project)
            .Include(i => i.Children).ThenInclude(c => c.TodoItemTags).ThenInclude(t => t.TodoTag)
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
        var projectId = await ValidateProjectOwnershipAsync(userId, request.ProjectId, ct);

        var maxOrder = await db.TodoItems
            .Where(i => i.UserId == userId && i.ProjectId == projectId)
            .Select(i => (int?)i.Order)
            .MaxAsync(ct) ?? -1;

        var utcNow = DateTime.UtcNow;
        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            ParentId = request.ParentId,
            CategoryId = request.CategoryId,
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

        if (request.TagIds?.Count > 0)
        {
            var tags = request.TagIds
                .Select(tagId => new TodoItemTag { TodoItemId = item.Id, TodoTagId = tagId });
            db.Set<TodoItemTag>().AddRange(tags);
        }

        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(item.Id, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> UpdateItemAsync(
        string userId, Guid itemId, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        var projectId = await ValidateProjectOwnershipAsync(userId, request.ProjectId, ct);
        var utcNow = DateTime.UtcNow;

        item.Title = request.Title;
        item.Description = request.Description;
        item.Priority = ParsePriority(request.Priority);
        item.Difficulty = ParseDifficulty(request.Difficulty);
        item.DueDate = request.DueDate;
        item.ScheduledDate = request.ScheduledDate;
        item.ProjectId = projectId;

        if (request.ParentId == itemId)
            throw new ArgumentException("任務不能設定自身為父任務");
        item.ParentId = request.ParentId;
        item.CategoryId = request.CategoryId;
        item.RecurrencePattern = ParseRecurrencePattern(request.RecurrencePattern);
        item.RecurrenceInterval = request.RecurrenceInterval;
        item.UpdatedAt = utcNow;
        SetItemStatus(item, ParseStatus(request.Status), utcNow);

        if (request.TagIds is not null)
        {
            var existing = await db.Set<TodoItemTag>()
                .Where(t => t.TodoItemId == itemId)
                .ToListAsync(ct);
            db.Set<TodoItemTag>().RemoveRange(existing);

            if (request.TagIds.Count > 0)
            {
                var newTags = request.TagIds
                    .Select(tagId => new TodoItemTag { TodoItemId = itemId, TodoTagId = tagId });
                db.Set<TodoItemTag>().AddRange(newTags);
            }
        }

        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> ToggleCompleteAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        ToggleCompletedState(item, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return await GetItemResponseWithTagsAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemDetailResponse> UpdateStatusAsync(
        string userId, Guid itemId, string status, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        var utcNow = DateTime.UtcNow;
        var newStatus = ParseStatusFromClient(status);

        SetItemStatus(item, newStatus, utcNow);
        item.UpdatedAt = utcNow;

        if (newStatus == TodoStatus.Done && item.RecurrencePattern != RecurrencePattern.None)
            CreateRecurringNext(item, utcNow);

        await db.SaveChangesAsync(ct);

        var detail = await GetItemDetailAsync(userId, itemId, ct);
        return detail!;
    }

    /// <inheritdoc />
    public async Task<ItemDetailResponse> ArchiveAsync(string userId, Guid itemId, bool archive, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        item.ArchivedAt = archive ? DateTime.UtcNow : null;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var detail = await GetItemDetailAsync(userId, itemId, ct);
        return detail!;
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetTrashAsync(string userId, CancellationToken ct = default)
        => await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId && i.DeletedAt != null)
            .OrderByDescending(i => i.DeletedAt)
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(ToItemResponseFromEntity).ToList(), ct);

    /// <inheritdoc />
    public async Task RestoreAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        item.DeletedAt = null;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task PermanentDeleteAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ChecklistItemResponse> AddChecklistItemAsync(
        string userId, Guid itemId, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        _ = await GetUserItemAsync(userId, itemId, ct);

        var checklist = new TodoChecklistItem
        {
            Id = Guid.NewGuid(),
            TodoItemId = itemId,
            Title = request.Title,
            IsCompleted = false,
            Order = request.Order,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.TodoChecklistItems.Add(checklist);
        await db.SaveChangesAsync(ct);
        return new ChecklistItemResponse(checklist.Id, checklist.Title, checklist.IsCompleted, checklist.Order);
    }

    /// <inheritdoc />
    public async Task<ChecklistItemResponse> UpdateChecklistItemAsync(
        string userId, Guid itemId, Guid checklistId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        _ = await GetUserItemAsync(userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("找不到指定的清單項目");

        if (request.Title is not null) checklist.Title = request.Title;
        if (request.IsCompleted.HasValue) checklist.IsCompleted = request.IsCompleted.Value;
        if (request.Order.HasValue) checklist.Order = request.Order.Value;
        checklist.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return new ChecklistItemResponse(checklist.Id, checklist.Title, checklist.IsCompleted, checklist.Order);
    }

    /// <inheritdoc />
    public async Task DeleteChecklistItemAsync(
        string userId, Guid itemId, Guid checklistId, CancellationToken ct = default)
    {
        _ = await GetUserItemAsync(userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("找不到指定的清單項目");

        db.TodoChecklistItems.Remove(checklist);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetTodayAsync(string userId, CancellationToken ct = default)
        => await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId && i.DeletedAt == null
                && (i.DueDate == Today || i.ScheduledDate == Today))
            .OrderBy(i => i.Order).ThenBy(i => i.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(ToItemResponseFromEntity).ToList(), ct);

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetWeekAsync(string userId, CancellationToken ct = default)
    {
        var weekEnd = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        return await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId && i.DeletedAt == null
                && ((i.DueDate >= Today && i.DueDate <= weekEnd)
                    || (i.ScheduledDate >= Today && i.ScheduledDate <= weekEnd)))
            .OrderBy(i => i.DueDate ?? i.ScheduledDate).ThenBy(i => i.Order)
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(ToItemResponseFromEntity).ToList(), ct);
    }

    /// <inheritdoc />
    public async Task<List<ItemResponse>> GetMonthAsync(string userId, CancellationToken ct = default)
    {
        var monthStart = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        var monthEnd = DateOnly.FromDateTime(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1));
        return await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId && i.DeletedAt == null
                && ((i.DueDate >= monthStart && i.DueDate <= monthEnd)
                    || (i.ScheduledDate >= monthStart && i.ScheduledDate <= monthEnd)))
            .OrderBy(i => i.DueDate ?? i.ScheduledDate).ThenBy(i => i.Order)
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(ToItemResponseFromEntity).ToList(), ct);
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
        => await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId && i.DeletedAt == null && i.Title.Contains(q))
            .OrderBy(i => i.Order).ThenBy(i => i.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith(t => t.Result.Select(ToItemResponseFromEntity).ToList(), ct);

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

    private async Task<ItemResponse> GetItemResponseWithTagsAsync(Guid itemId, string userId, CancellationToken ct)
    {
        var item = await db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .FirstAsync(i => i.Id == itemId && i.UserId == userId, ct);

        return ToItemResponseFromEntity(item);
    }

    private IQueryable<TodoItem> ApplyViewFilter(
        IQueryable<TodoItem> query, string? view, Guid? projectId)
        => view?.ToLowerInvariant() switch
        {
            "inbox" => query.Where(i => i.ProjectId == null && i.Status != TodoStatus.Done),
            "today" => query.Where(i => i.DueDate == Today && i.Status != TodoStatus.Done),
            "upcoming" => query.Where(i => i.DueDate > Today && i.DueDate <= UpcomingEnd && i.Status != TodoStatus.Done),
            _ when projectId.HasValue => query.Where(i => i.ProjectId == projectId),
            _ => query,
        };

    private async Task<Guid?> ValidateProjectOwnershipAsync(string userId, Guid? projectId, CancellationToken ct)
    {
        if (projectId is null) return null;

        var exists = await db.TodoProjects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.UserId == userId, ct);

        return exists
            ? projectId
            : throw new KeyNotFoundException("找不到指定的專案");
    }

    private async Task<TodoItem> GetUserItemAsync(string userId, Guid itemId, CancellationToken ct)
        => await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId && i.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

    private void CreateRecurringNext(TodoItem item, DateTime utcNow)
    {
        var baseDate = item.ScheduledDate ?? item.DueDate;
        DateOnly? nextDate = baseDate is null ? null : item.RecurrencePattern switch
        {
            RecurrencePattern.Daily => baseDate.Value.AddDays(item.RecurrenceInterval),
            RecurrencePattern.Weekly => baseDate.Value.AddDays(7 * item.RecurrenceInterval),
            RecurrencePattern.Monthly => baseDate.Value.AddMonths(item.RecurrenceInterval),
            RecurrencePattern.Yearly => baseDate.Value.AddYears(item.RecurrenceInterval),
            _ => null,
        };

        var next = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = item.UserId,
            ProjectId = item.ProjectId,
            ParentId = item.ParentId,
            CategoryId = item.CategoryId,
            Title = item.Title,
            Description = item.Description,
            Priority = item.Priority,
            Difficulty = item.Difficulty,
            Status = TodoStatus.Pending,
            DueDate = item.DueDate.HasValue ? nextDate : null,
            ScheduledDate = item.ScheduledDate.HasValue ? nextDate : null,
            RecurrencePattern = item.RecurrencePattern,
            RecurrenceInterval = item.RecurrenceInterval,
            Order = item.Order,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        db.TodoItems.Add(next);
    }

    private static TodoPriority ParsePriority(string priority)
        => Enum.TryParse<TodoPriority>(priority, ignoreCase: true, out var p) ? p : TodoPriority.None;

    private static TodoStatus ParseStatus(string status)
        => Enum.TryParse<TodoStatus>(status, ignoreCase: true, out var s) ? s : TodoStatus.Pending;

    private static TodoStatus ParseStatusFromClient(string status)
        => status.ToLowerInvariant() switch
        {
            "completed" => TodoStatus.Done,
            "pending" => TodoStatus.Pending,
            "inprogress" => TodoStatus.InProgress,
            _ => Enum.TryParse<TodoStatus>(status, ignoreCase: true, out var s)
                ? s
                : throw new ArgumentException($"無效的狀態值：{status}"),
        };

    private static TodoDifficulty ParseDifficulty(string difficulty)
        => Enum.TryParse<TodoDifficulty>(difficulty, ignoreCase: true, out var d) ? d : TodoDifficulty.None;

    private static RecurrencePattern ParseRecurrencePattern(string pattern)
        => Enum.TryParse<RecurrencePattern>(pattern, ignoreCase: true, out var p) ? p : RecurrencePattern.None;

    private static void SetItemStatus(TodoItem item, TodoStatus status, DateTime utcNow)
    {
        if (status is TodoStatus.Done)
        {
            if (item.Status is TodoStatus.Done) return;
            item.Status = TodoStatus.Done;
            item.CompletedAt = utcNow;
            return;
        }

        item.Status = status;
        item.CompletedAt = null;
    }

    private static void ToggleCompletedState(TodoItem item, DateTime utcNow)
    {
        if (item.Status is TodoStatus.Done)
        {
            item.Status = TodoStatus.Pending;
            item.CompletedAt = null;
        }
        else
        {
            item.Status = TodoStatus.Done;
            item.CompletedAt = utcNow;
        }

        item.UpdatedAt = utcNow;
    }

    private static ItemResponse ToItemResponseFromEntity(TodoItem i)
        => new(
            i.Id,
            i.Title,
            i.Description,
            i.Priority.ToString(),
            i.Status.ToString(),  // ItemResponse 保持原始 enum 名稱（"Done"）
            i.Difficulty.ToString(),
            i.DueDate,
            i.ScheduledDate,
            i.ProjectId,
            i.Project?.Name,
            i.Project?.Color,
            i.ParentId,
            i.Order,
            i.CreatedAt,
            i.CompletedAt,
            i.ArchivedAt,
            i.TodoItemTags.Select(t => new TagResponse(t.TodoTag.Id, t.TodoTag.Name, t.TodoTag.Color)).ToList());

    private static ItemDetailResponse ToItemDetailResponse(TodoItem i)
        => new(
            i.Id,
            i.Title,
            i.Description,
            i.Priority.ToString(),
            MapStatusToClient(i.Status),
            i.Difficulty.ToString(),
            i.DueDate,
            i.ScheduledDate,
            i.ProjectId,
            i.Project?.Name,
            i.Project?.Color,
            i.ParentId,
            i.CategoryId,
            i.Category?.Name,
            i.Order,
            i.RecurrencePattern.ToString(),
            i.RecurrenceInterval,
            i.CreatedAt,
            i.CompletedAt,
            i.ArchivedAt,
            i.Children.Select(ToItemResponseFromEntity).ToList(),
            i.ChecklistItems.OrderBy(c => c.Order).Select(c => new ChecklistItemResponse(c.Id, c.Title, c.IsCompleted, c.Order)).ToList(),
            i.TodoItemTags.Select(t => new TagResponse(t.TodoTag.Id, t.TodoTag.Name, t.TodoTag.Color)).ToList());

    /// <summary>舊端點仍保持 "Done"；新端點用此方法回傳 "Completed"</summary>
    private static string MapStatusToClient(TodoStatus status)
        => status == TodoStatus.Done ? "Completed" : status.ToString();
}
