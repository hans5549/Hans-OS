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
    public async Task<List<ItemResponse>> GetItemsAsync(
        string userId,
        TodoViewFilter? view = null,
        Guid? projectId = null,
        CancellationToken ct = default)
        => await ApplyViewFilter(
                db.TodoItems
                    .AsNoTracking()
                    .Where(i => i.UserId == userId),
                view,
                projectId)
            .OrderBy(i => i.Order)
            .ThenBy(i => i.CreatedAt)
            .Select(ToItemResponse())
            .ToListAsync(ct);

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
        var projectId = await ValidateProjectOwnershipAsync(userId, request.ProjectId, ct);

        var maxOrder = await db.TodoItems
            .Where(i => i.UserId == userId && i.ProjectId == projectId)
            .Select(i => (int?)i.Order)
            .MaxAsync(ct) ?? -1;

        var item = CreateItem(userId, request, projectId, maxOrder + 1, DateTime.UtcNow);
        db.TodoItems.Add(item);
        await db.SaveChangesAsync(ct);
        return await GetItemResponseAsync(item.Id, userId, ct);
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
        item.DueDate = request.DueDate;
        item.ProjectId = projectId;
        item.UpdatedAt = utcNow;
        SetItemStatus(item, ParseStatus(request.Status), utcNow);

        await db.SaveChangesAsync(ct);

        return await GetItemResponseAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task<ItemResponse> ToggleCompleteAsync(
        string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);
        ToggleCompletedState(item, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return await GetItemResponseAsync(itemId, userId, ct);
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await GetUserItemAsync(userId, itemId, ct);

        db.TodoItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ItemResponse> GetItemResponseAsync(Guid itemId, string userId, CancellationToken ct)
        => await db.TodoItems
            .AsNoTracking()
            .Where(i => i.Id == itemId && i.UserId == userId)
            .Select(ToItemResponse())
            .FirstAsync(ct);

    private IQueryable<TodoItem> ApplyViewFilter(
        IQueryable<TodoItem> query,
        TodoViewFilter? view,
        Guid? projectId)
        => view switch
        {
            TodoViewFilter.Inbox => query.Where(i => i.ProjectId == null && i.Status != TodoStatus.Done),
            TodoViewFilter.Today => query.Where(i => i.DueDate == Today && i.Status != TodoStatus.Done),
            TodoViewFilter.Upcoming => query.Where(i => i.DueDate > Today && i.DueDate <= UpcomingEnd && i.Status != TodoStatus.Done),
            TodoViewFilter.All => query,
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
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");

    private static TodoPriority ParsePriority(string priority)
        => Enum.TryParse<TodoPriority>(priority, ignoreCase: true, out var parsedPriority)
            ? parsedPriority
            : TodoPriority.None;

    private static TodoStatus ParseStatus(string status)
        => Enum.TryParse<TodoStatus>(status, ignoreCase: true, out var parsedStatus)
            ? parsedStatus
            : TodoStatus.Pending;

    private static TodoItem CreateItem(string userId, CreateItemRequest request, Guid? projectId, int order, DateTime utcNow)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            Title = request.Title,
            Description = request.Description,
            Priority = ParsePriority(request.Priority),
            Status = TodoStatus.Pending,
            DueDate = request.DueDate,
            Order = order,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

    private static void SetItemStatus(TodoItem item, TodoStatus status, DateTime utcNow)
    {
        if (status is TodoStatus.Done)
        {
            if (item.Status is TodoStatus.Done)
            {
                return;
            }

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

    private static Expression<Func<TodoItem, ItemResponse>> ToItemResponse()
        => i => new ItemResponse(
            i.Id,
            i.Title,
            i.Description,
            i.Priority.ToString(),
            i.Status.ToString(),
            i.DueDate,
            i.ProjectId,
            i.Project == null ? null : i.Project.Name,
            i.Project == null ? null : i.Project.Color,
            i.Order,
            i.CreatedAt,
            i.CompletedAt);
}
