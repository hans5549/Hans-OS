using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todo;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>待辦任務核心 CRUD 服務</summary>
public class TodoItemService(
    ApplicationDbContext db,
    ILogger<TodoItemService> logger) : ITodoItemService
{
    // ── List (Paged + Filtered) ─────────────────────

    public async Task<PagedResponse<TodoItemResponse>> GetItemsAsync(
        string userId, TodoQueryParams query, CancellationToken ct = default)
    {
        var q = db.TodoItems
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (!query.IncludeArchived)
            q = q.Where(t => t.ArchivedAt == null);

        if (query.TopLevelOnly == true)
            q = q.Where(t => t.ParentId == null);
        else if (query.ParentId.HasValue)
            q = q.Where(t => t.ParentId == query.ParentId.Value);

        q = ApplyFilters(q, query);
        q = ApplySorting(q, query.SortBy, query.SortDir);

        var totalCount = await q.CountAsync(ct);
        var entities = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .Include(t => t.Children)
            .Include(t => t.ChecklistItems)
            .ToListAsync(ct);

        var items = entities.Select(MapToResponse).ToList();

        return new PagedResponse<TodoItemResponse>(items, totalCount, query.Page, query.PageSize);
    }

    // ── Get Single ──────────────────────────────────

    public async Task<TodoItemDetailResponse> GetItemAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .Include(t => t.Children).ThenInclude(c => c.Category)
            .Include(t => t.Children).ThenInclude(c => c.Tags)
            .Include(t => t.Children).ThenInclude(c => c.Children)
            .Include(t => t.Children).ThenInclude(c => c.ChecklistItems)
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("任務不存在");

        return new TodoItemDetailResponse(
            item.Id, item.Title, item.Description,
            item.Status.ToString(), item.Priority.ToString(), item.Difficulty.ToString(),
            item.ScheduledDate, item.DueDate, item.ReminderAt,
            item.SortOrder,
            item.RecurrencePattern?.ToString(), item.RecurrenceInterval, item.RecurrenceSourceId,
            item.ParentId, item.CategoryId,
            item.Category?.Name, item.Category?.Color,
            item.Tags.Select(t => new TodoTagResponse(t.Id, t.Name, t.Color, t.CreatedAt)).ToList(),
            item.Children
                .OrderBy(c => c.SortOrder)
                .Select(c => MapToResponse(c))
                .ToList(),
            item.ChecklistItems
                .OrderBy(c => c.SortOrder)
                .Select(c => new ChecklistItemResponse(c.Id, c.Title, c.IsCompleted, c.SortOrder))
                .ToList(),
            item.CompletedAt, item.ArchivedAt, item.CreatedAt, item.UpdatedAt);
    }

    // ── Create ──────────────────────────────────────

    public async Task<TodoItemResponse> CreateItemAsync(
        string userId, CreateTodoItemRequest request, CancellationToken ct = default)
    {
        if (request.ParentId.HasValue)
        {
            var parentExists = await db.TodoItems
                .AnyAsync(t => t.Id == request.ParentId.Value && t.UserId == userId, ct);
            if (!parentExists)
                throw new KeyNotFoundException("父任務不存在");
        }

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Status = ParseEnum<TodoStatus>(request.Status, TodoStatus.Pending),
            Priority = ParseEnum<TodoPriority>(request.Priority, TodoPriority.Medium),
            Difficulty = ParseEnum<TodoDifficulty>(request.Difficulty, TodoDifficulty.Medium),
            ScheduledDate = request.ScheduledDate,
            DueDate = request.DueDate,
            ReminderAt = request.ReminderAt,
            ParentId = request.ParentId,
            CategoryId = request.CategoryId,
            RecurrencePattern = string.IsNullOrEmpty(request.RecurrencePattern)
                ? null
                : ParseEnum<RecurrencePattern>(request.RecurrencePattern),
            RecurrenceInterval = request.RecurrenceInterval < 1 ? 1 : request.RecurrenceInterval,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        if (request.TagIds is { Count: > 0 })
        {
            var tags = await db.TodoTags
                .Where(t => request.TagIds.Contains(t.Id) && t.UserId == userId)
                .ToListAsync(ct);
            item.Tags = tags;
        }

        db.TodoItems.Add(item);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦任務已建立: {ItemId}, 標題={Title}", item.Id, item.Title);

        return await LoadAndMapResponseAsync(item.Id, ct);
    }

    // ── Update ──────────────────────────────────────

    public async Task<TodoItemResponse> UpdateItemAsync(
        string userId, Guid itemId, UpdateTodoItemRequest request, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("任務不存在");

        if (request.ParentId.HasValue && request.ParentId.Value != item.ParentId)
        {
            if (request.ParentId.Value == itemId)
                throw new ArgumentException("任務不能成為自己的子任務");

            var parentExists = await db.TodoItems
                .AnyAsync(t => t.Id == request.ParentId.Value && t.UserId == userId, ct);
            if (!parentExists)
                throw new KeyNotFoundException("父任務不存在");
        }

        item.Title = request.Title;
        item.Description = request.Description;
        item.Priority = ParseEnum<TodoPriority>(request.Priority, item.Priority);
        item.Difficulty = ParseEnum<TodoDifficulty>(request.Difficulty, item.Difficulty);
        item.ScheduledDate = request.ScheduledDate;
        item.DueDate = request.DueDate;
        item.ReminderAt = request.ReminderAt;
        item.ParentId = request.ParentId;
        item.CategoryId = request.CategoryId;
        item.RecurrencePattern = string.IsNullOrEmpty(request.RecurrencePattern)
            ? null
            : ParseEnum<RecurrencePattern>(request.RecurrencePattern);
        item.RecurrenceInterval = request.RecurrenceInterval < 1 ? 1 : request.RecurrenceInterval;
        item.UpdatedAt = DateTime.UtcNow;

        if (request.TagIds is not null)
        {
            item.Tags.Clear();
            if (request.TagIds.Count > 0)
            {
                var tags = await db.TodoTags
                    .Where(t => request.TagIds.Contains(t.Id) && t.UserId == userId)
                    .ToListAsync(ct);
                item.Tags = tags;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦任務已更新: {ItemId}", itemId);

        return await LoadAndMapResponseAsync(item.Id, ct);
    }

    // ── Soft Delete ─────────────────────────────────

    public async Task DeleteItemAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("任務不存在");

        item.DeletedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦任務已軟刪除: {ItemId}", itemId);
    }

    // ── Status Change ───────────────────────────────

    public async Task<TodoItemResponse> UpdateStatusAsync(
        string userId, Guid itemId, UpdateTodoStatusRequest request, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("任務不存在");

        var newStatus = ParseEnum<TodoStatus>(request.Status);

        item.Status = newStatus;
        item.UpdatedAt = DateTime.UtcNow;

        if (newStatus == TodoStatus.Completed)
        {
            item.CompletedAt = DateTime.UtcNow;

            // 週期性任務：自動產生下一期
            if (item.RecurrencePattern.HasValue)
            {
                await CreateNextRecurrenceAsync(item, ct);
            }
        }
        else
        {
            item.CompletedAt = null;
        }

        await db.SaveChangesAsync(ct);

        return await LoadAndMapResponseAsync(item.Id, ct);
    }

    // ── Archive ─────────────────────────────────────

    public async Task<TodoItemResponse> ArchiveItemAsync(
        string userId, Guid itemId, ArchiveTodoRequest request, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("任務不存在");

        item.ArchivedAt = request.Archive ? DateTime.UtcNow : null;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await LoadAndMapResponseAsync(item.Id, ct);
    }

    // ── Restore from Trash ──────────────────────────

    public async Task RestoreItemAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId && t.DeletedAt != null, ct)
            ?? throw new KeyNotFoundException("垃圾桶中找不到此任務");

        item.DeletedAt = null;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Permanent Delete ────────────────────────────

    public async Task PermanentDeleteAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == itemId && t.UserId == userId && t.DeletedAt != null, ct)
            ?? throw new KeyNotFoundException("垃圾桶中找不到此任務");

        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦任務已永久刪除: {ItemId}", itemId);
    }

    // ── Today / Week / Month ────────────────────────

    public async Task<List<TodoItemResponse>> GetTodayItemsAsync(
        string userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await GetTimeRangeItemsAsync(userId, today, today, ct);
    }

    public async Task<List<TodoItemResponse>> GetWeekItemsAsync(
        string userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
        var endOfWeek = startOfWeek.AddDays(6);
        return await GetTimeRangeItemsAsync(userId, startOfWeek, endOfWeek, ct);
    }

    public async Task<List<TodoItemResponse>> GetMonthItemsAsync(
        string userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return await GetTimeRangeItemsAsync(userId, startOfMonth, endOfMonth, ct);
    }

    // ── Stats ───────────────────────────────────────

    public async Task<TodoStatsResponse> GetStatsAsync(
        string userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await db.TodoItems
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Status, t.DueDate, t.ArchivedAt })
            .ToListAsync(ct);

        return new TodoStatsResponse(
            Total: items.Count(i => i.ArchivedAt == null),
            Pending: items.Count(i => i.Status == TodoStatus.Pending && i.ArchivedAt == null),
            InProgress: items.Count(i => i.Status == TodoStatus.InProgress && i.ArchivedAt == null),
            Completed: items.Count(i => i.Status == TodoStatus.Completed && i.ArchivedAt == null),
            Overdue: items.Count(i => i.DueDate.HasValue && i.DueDate.Value < today
                && i.Status != TodoStatus.Completed && i.ArchivedAt == null),
            Archived: items.Count(i => i.ArchivedAt != null));
    }

    // ── Reminder Count (Badge) ──────────────────────

    public async Task<int> GetReminderCountAsync(
        string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.TodoItems
            .AsNoTracking()
            .CountAsync(t => t.UserId == userId
                && t.ReminderAt != null
                && t.ReminderAt <= now
                && t.Status != TodoStatus.Completed, ct);
    }

    // ── Trash ───────────────────────────────────────

    public async Task<List<TodoItemResponse>> GetTrashAsync(
        string userId, CancellationToken ct = default)
    {
        var entities = await db.TodoItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.DeletedAt != null)
            .OrderByDescending(t => t.DeletedAt)
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .ToListAsync(ct);

        return entities.Select(t => new TodoItemResponse(
            t.Id, t.Title, t.Description,
            t.Status.ToString(), t.Priority.ToString(), t.Difficulty.ToString(),
            t.ScheduledDate, t.DueDate, t.ReminderAt,
            t.SortOrder,
            t.RecurrencePattern?.ToString(), t.RecurrenceInterval, t.RecurrenceSourceId,
            t.ParentId, t.CategoryId,
            t.Category?.Name, t.Category?.Color,
            t.Tags.Select(tag => new TodoTagResponse(tag.Id, tag.Name, tag.Color, tag.CreatedAt)).ToList(),
            0, 0, 0, 0,
            t.CompletedAt, t.ArchivedAt, t.CreatedAt, t.UpdatedAt)).ToList();
    }

    // ── Search ──────────────────────────────────────

    public async Task<List<TodoItemResponse>> SearchAsync(
        string userId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var searchLower = query.Trim().ToLowerInvariant();
        var entities = await db.TodoItems
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ArchivedAt == null)
            .Where(t => t.Title.ToLower().Contains(searchLower)
                || (t.Description != null && t.Description.ToLower().Contains(searchLower))
                || t.Tags.Any(tag => tag.Name.ToLower().Contains(searchLower)))
            .OrderByDescending(t => t.UpdatedAt)
            .Take(50)
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .Include(t => t.Children)
            .Include(t => t.ChecklistItems)
            .ToListAsync(ct);

        return entities.Select(MapToResponse).ToList();
    }

    // ── Checklist CRUD ──────────────────────────────

    public async Task<ChecklistItemResponse> AddChecklistItemAsync(
        string userId, Guid itemId, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        await EnsureItemExistsAsync(userId, itemId, ct);

        var maxSort = await db.TodoChecklistItems
            .Where(c => c.TodoItemId == itemId)
            .MaxAsync(c => (int?)c.SortOrder, ct) ?? -1;

        var checklist = new TodoChecklistItem
        {
            Id = Guid.NewGuid(),
            TodoItemId = itemId,
            Title = request.Title,
            IsCompleted = false,
            SortOrder = maxSort + 1,
        };

        db.TodoChecklistItems.Add(checklist);
        await db.SaveChangesAsync(ct);

        return new ChecklistItemResponse(checklist.Id, checklist.Title, checklist.IsCompleted, checklist.SortOrder);
    }

    public async Task<ChecklistItemResponse> UpdateChecklistItemAsync(
        string userId, Guid itemId, Guid checklistId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        await EnsureItemExistsAsync(userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("核取清單項目不存在");

        if (request.Title is not null) checklist.Title = request.Title;
        if (request.IsCompleted.HasValue) checklist.IsCompleted = request.IsCompleted.Value;

        await db.SaveChangesAsync(ct);

        return new ChecklistItemResponse(checklist.Id, checklist.Title, checklist.IsCompleted, checklist.SortOrder);
    }

    public async Task DeleteChecklistItemAsync(
        string userId, Guid itemId, Guid checklistId, CancellationToken ct = default)
    {
        await EnsureItemExistsAsync(userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("核取清單項目不存在");

        db.TodoChecklistItems.Remove(checklist);
        await db.SaveChangesAsync(ct);
    }

    // ── Batch Update ────────────────────────────────

    public async Task BatchUpdateAsync(
        string userId, BatchUpdateTodoRequest request, CancellationToken ct = default)
    {
        var items = await db.TodoItems
            .Where(t => request.Ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync(ct);

        if (items.Count == 0)
            throw new KeyNotFoundException("找不到指定的任務");

        foreach (var item in items)
        {
            if (request.Status is not null)
            {
                item.Status = ParseEnum<TodoStatus>(request.Status);
                item.CompletedAt = item.Status == TodoStatus.Completed ? DateTime.UtcNow : null;
            }
            if (request.Priority is not null)
                item.Priority = ParseEnum<TodoPriority>(request.Priority);
            if (request.CategoryId.HasValue)
                item.CategoryId = request.CategoryId.Value;
            if (request.Archive == true)
                item.ArchivedAt = DateTime.UtcNow;
            if (request.Delete == true)
                item.DeletedAt = DateTime.UtcNow;

            item.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("批次更新 {Count} 個任務", items.Count);
    }

    // ── Sort ────────────────────────────────────────

    public async Task SortItemsAsync(
        string userId, SortTodoRequest request, CancellationToken ct = default)
    {
        var items = await db.TodoItems
            .Where(t => request.OrderedIds.Contains(t.Id)
                && t.UserId == userId
                && t.ParentId == request.ParentId)
            .ToListAsync(ct);

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var item = items.FirstOrDefault(t => t.Id == request.OrderedIds[i]);
            if (item is not null)
            {
                item.SortOrder = i;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Private Helpers ─────────────────────────────

    private async Task<List<TodoItemResponse>> GetTimeRangeItemsAsync(
        string userId, DateOnly start, DateOnly end, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var entities = await db.TodoItems
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.ArchivedAt == null
                && t.Status != TodoStatus.Completed
                && (
                    (t.DueDate.HasValue && t.DueDate.Value >= start && t.DueDate.Value <= end)
                    || (t.ScheduledDate.HasValue && t.ScheduledDate.Value >= start && t.ScheduledDate.Value <= end)
                    || (start == today && t.DueDate.HasValue && t.DueDate.Value < today)
                ))
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .Include(t => t.Children)
            .Include(t => t.ChecklistItems)
            .ToListAsync(ct);

        return entities.Select(MapToResponse).ToList();
    }

    private static IQueryable<TodoItem> ApplyFilters(IQueryable<TodoItem> q, TodoQueryParams query)
    {
        if (!string.IsNullOrEmpty(query.Status))
        {
            var statuses = query.Status.Split(',')
                .Select(s => Enum.Parse<TodoStatus>(s.Trim(), ignoreCase: true))
                .ToList();
            q = q.Where(t => statuses.Contains(t.Status));
        }

        if (!string.IsNullOrEmpty(query.Priority))
        {
            var priorities = query.Priority.Split(',')
                .Select(s => Enum.Parse<TodoPriority>(s.Trim(), ignoreCase: true))
                .ToList();
            q = q.Where(t => priorities.Contains(t.Priority));
        }

        if (!string.IsNullOrEmpty(query.Difficulty))
        {
            var difficulties = query.Difficulty.Split(',')
                .Select(s => Enum.Parse<TodoDifficulty>(s.Trim(), ignoreCase: true))
                .ToList();
            q = q.Where(t => difficulties.Contains(t.Difficulty));
        }

        if (query.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrEmpty(query.TagIds))
        {
            var tagIds = query.TagIds.Split(',').Select(Guid.Parse).ToList();
            q = q.Where(t => t.Tags.Any(tag => tagIds.Contains(tag.Id)));
        }

        if (query.Overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value < today
                && t.Status != TodoStatus.Completed);
        }

        if (!string.IsNullOrEmpty(query.Search))
        {
            var searchLower = query.Search.Trim().ToLowerInvariant();
            q = q.Where(t => t.Title.ToLower().Contains(searchLower)
                || (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }

        return q;
    }

    private static IQueryable<TodoItem> ApplySorting(IQueryable<TodoItem> q, string sortBy, string sortDir)
    {
        var desc = sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "duedate" => desc ? q.OrderByDescending(t => t.DueDate) : q.OrderBy(t => t.DueDate),
            "priority" => desc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "createdat" => desc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
            "updatedat" => desc ? q.OrderByDescending(t => t.UpdatedAt) : q.OrderBy(t => t.UpdatedAt),
            _ => desc ? q.OrderByDescending(t => t.SortOrder) : q.OrderBy(t => t.SortOrder),
        };
    }

    private static TodoItemResponse MapToResponse(TodoItem t)
    {
        return new TodoItemResponse(
            t.Id, t.Title, t.Description,
            t.Status.ToString(), t.Priority.ToString(), t.Difficulty.ToString(),
            t.ScheduledDate, t.DueDate, t.ReminderAt,
            t.SortOrder,
            t.RecurrencePattern?.ToString(), t.RecurrenceInterval, t.RecurrenceSourceId,
            t.ParentId, t.CategoryId,
            t.Category?.Name, t.Category?.Color,
            t.Tags.Select(tag => new TodoTagResponse(tag.Id, tag.Name, tag.Color, tag.CreatedAt)).ToList(),
            t.Children.Count,
            t.Children.Count(c => c.Status == TodoStatus.Completed),
            t.ChecklistItems.Count,
            t.ChecklistItems.Count(c => c.IsCompleted),
            t.CompletedAt, t.ArchivedAt, t.CreatedAt, t.UpdatedAt);
    }

    private async Task<TodoItemResponse> LoadAndMapResponseAsync(Guid itemId, CancellationToken ct)
    {
        var item = await db.TodoItems
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .Include(t => t.Children)
            .Include(t => t.ChecklistItems)
            .FirstAsync(t => t.Id == itemId, ct);

        return MapToResponse(item);
    }

    private async Task EnsureItemExistsAsync(string userId, Guid itemId, CancellationToken ct)
    {
        var exists = await db.TodoItems
            .AnyAsync(t => t.Id == itemId && t.UserId == userId, ct);
        if (!exists)
            throw new KeyNotFoundException("任務不存在");
    }

    private async Task CreateNextRecurrenceAsync(TodoItem completedItem, CancellationToken ct)
    {
        var nextScheduled = CalculateNextDate(
            completedItem.ScheduledDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            completedItem.RecurrencePattern!.Value,
            completedItem.RecurrenceInterval);

        DateOnly? nextDue = null;
        if (completedItem.ScheduledDate.HasValue && completedItem.DueDate.HasValue)
        {
            var duration = completedItem.DueDate.Value.DayNumber - completedItem.ScheduledDate.Value.DayNumber;
            nextDue = nextScheduled.AddDays(duration);
        }
        else if (completedItem.DueDate.HasValue)
        {
            nextDue = CalculateNextDate(
                completedItem.DueDate.Value,
                completedItem.RecurrencePattern!.Value,
                completedItem.RecurrenceInterval);
        }

        var nextItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = completedItem.UserId,
            Title = completedItem.Title,
            Description = completedItem.Description,
            Status = TodoStatus.Pending,
            Priority = completedItem.Priority,
            Difficulty = completedItem.Difficulty,
            ScheduledDate = nextScheduled,
            DueDate = nextDue,
            ReminderAt = null,
            SortOrder = completedItem.SortOrder,
            RecurrencePattern = completedItem.RecurrencePattern,
            RecurrenceInterval = completedItem.RecurrenceInterval,
            RecurrenceSourceId = completedItem.RecurrenceSourceId ?? completedItem.Id,
            ParentId = completedItem.ParentId,
            CategoryId = completedItem.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.TodoItems.Add(nextItem);

        logger.LogInformation("週期性任務已產生下一期: {NewItemId}, 來源={SourceId}",
            nextItem.Id, completedItem.Id);
    }

    private static DateOnly CalculateNextDate(DateOnly current, RecurrencePattern pattern, int interval)
    {
        return pattern switch
        {
            Data.Entities.RecurrencePattern.Daily => current.AddDays(interval),
            Data.Entities.RecurrencePattern.Weekly => current.AddDays(7 * interval),
            Data.Entities.RecurrencePattern.Monthly => current.AddMonths(interval),
            Data.Entities.RecurrencePattern.Yearly => current.AddYears(interval),
            _ => current.AddDays(interval),
        };
    }

    private static T ParseEnum<T>(string? value, T defaultValue = default) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }

    private static T ParseEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException($"'{typeof(T).Name}' 值不可為空");
        if (!Enum.TryParse<T>(value, ignoreCase: true, out var result))
            throw new ArgumentException($"無效的 {typeof(T).Name} 值: {value}");
        return result;
    }
}
