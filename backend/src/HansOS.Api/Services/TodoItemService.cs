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
    public async Task<List<ItemResponse>> GetItemsAsync(
        string userId,
        TodoViewFilter? view = null,
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        var query = db.TodoItems
            .AsNoTracking()
            .Where(i => i.UserId == userId);

        query = view switch
        {
            TodoViewFilter.Inbox => query.Where(i => i.ProjectId == null && i.Status != TodoStatus.Done),
            TodoViewFilter.Today => query.Where(i => i.DueDate == Today && i.Status != TodoStatus.Done),
            TodoViewFilter.Upcoming => query.Where(i => i.DueDate > Today && i.DueDate <= UpcomingEnd && i.Status != TodoStatus.Done),
            TodoViewFilter.All => query,
            _ when projectId.HasValue => query.Where(i => i.ProjectId == projectId),
            _ => query,
        };

        return await query
            .OrderBy(i => i.Order)
            .ThenBy(i => i.CreatedAt)
            .Select(i => new ItemResponse(
                i.Id,
                i.Title,
                i.Description,
                i.Priority.ToString(),
                i.Status.ToString(),
                i.DueDate,
                i.ProjectId,
                i.Project != null ? i.Project.Name : null,
                i.Project != null ? i.Project.Color : null,
                i.Order,
                i.CreatedAt,
                i.CompletedAt))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TodoCountsResponse> GetCountsAsync(string userId, CancellationToken ct = default)
    {
        var counts = await db.TodoItems
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Status != TodoStatus.Done)
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
        if (!Enum.TryParse<TodoPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            priority = TodoPriority.None;
        }

        var maxOrder = await db.TodoItems
            .Where(i => i.UserId == userId && i.ProjectId == request.ProjectId)
            .Select(i => (int?)i.Order)
            .MaxAsync(ct) ?? -1;

        var item = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Priority = priority,
            Status = TodoStatus.Pending,
            DueDate = request.DueDate,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.TodoItems.Add(item);
        await db.SaveChangesAsync(ct);

        return await GetItemResponseAsync(item.Id, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> UpdateItemAsync(
        string userId, Guid itemId, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        if (!Enum.TryParse<TodoPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            priority = TodoPriority.None;
        }

        if (!Enum.TryParse<TodoStatus>(request.Status, ignoreCase: true, out var status))
        {
            status = TodoStatus.Pending;
        }

        item.Title = request.Title;
        item.Description = request.Description;
        item.Priority = priority;
        item.DueDate = request.DueDate;
        item.ProjectId = request.ProjectId;
        item.UpdatedAt = DateTime.UtcNow;

        if (status == TodoStatus.Done && item.Status != TodoStatus.Done)
        {
            item.Status = TodoStatus.Done;
            item.CompletedAt = DateTime.UtcNow;
        }
        else if (status != TodoStatus.Done)
        {
            item.Status = status;
            item.CompletedAt = null;
        }

        await db.SaveChangesAsync(ct);

        return await GetItemResponseAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> ToggleCompleteAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        if (item.Status == TodoStatus.Done)
        {
            item.Status = TodoStatus.Pending;
            item.CompletedAt = null;
        }
        else
        {
            item.Status = TodoStatus.Done;
            item.CompletedAt = DateTime.UtcNow;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetItemResponseAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ItemResponse> GetItemResponseAsync(Guid itemId, string userId, CancellationToken ct)
    {
        return await db.TodoItems
            .AsNoTracking()
            .Where(i => i.Id == itemId && i.UserId == userId)
            .Select(i => new ItemResponse(
                i.Id,
                i.Title,
                i.Description,
                i.Priority.ToString(),
                i.Status.ToString(),
                i.DueDate,
                i.ProjectId,
                i.Project != null ? i.Project.Name : null,
                i.Project != null ? i.Project.Color : null,
                i.Order,
                i.CreatedAt,
                i.CompletedAt))
            .FirstAsync(ct);
    }
}
