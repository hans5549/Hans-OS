using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;
using static HansOS.Api.Services.Internal.TodoItemMapper;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemQueries
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public static DateOnly UpcomingEnd => DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    public static IQueryable<TodoItem> ActiveItems(ApplicationDbContext db, string userId)
        => ListItems(db, userId)
            .Where(i => i.DeletedAt == null && i.ArchivedAt == null);

    public static IQueryable<TodoItem> ListItems(ApplicationDbContext db, string userId)
        => db.TodoItems
            .AsNoTracking()
            .Include(i => i.Project)
            .Include(i => i.TodoItemTags).ThenInclude(t => t.TodoTag)
            .Where(i => i.UserId == userId);

    public static IQueryable<TodoItem> ApplyViewFilter(
        IQueryable<TodoItem> query,
        string? view,
        Guid? projectId)
        => view?.ToLowerInvariant() switch
        {
            "inbox" => query.Where(i => i.ProjectId == null && i.Status != TodoStatus.Done),
            "today" => query.Where(i => i.DueDate == Today && i.Status != TodoStatus.Done),
            "upcoming" => query.Where(i => i.DueDate > Today && i.DueDate <= UpcomingEnd && i.Status != TodoStatus.Done),
            _ when projectId.HasValue => query.Where(i => i.ProjectId == projectId),
            _ => query,
        };

    public static async Task<ItemResponse> GetItemResponseWithTagsAsync(
        ApplicationDbContext db,
        Guid itemId,
        string userId,
        CancellationToken ct)
    {
        var item = await ListItems(db, userId)
            .FirstAsync(i => i.Id == itemId, ct);

        return ToItemResponseFromEntity(item);
    }

    public static async Task<List<ItemResponse>> ToItemResponsesAsync(
        IQueryable<TodoItem> query,
        CancellationToken ct)
    {
        var items = await query.ToListAsync(ct);
        return items.Select(ToItemResponseFromEntity).ToList();
    }

    public static async Task<Guid?> ValidateProjectOwnershipAsync(
        ApplicationDbContext db,
        string userId,
        Guid? projectId,
        CancellationToken ct)
    {
        if (projectId is null) return null;

        var exists = await db.TodoProjects
            .AsNoTracking()
            .AnyAsync(p => p.Id == projectId && p.UserId == userId, ct);

        return exists
            ? projectId
            : throw new KeyNotFoundException("找不到指定的專案");
    }

    public static async Task<TodoItem> GetUserItemAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        CancellationToken ct)
        => await db.TodoItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId && i.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("找不到指定的任務");
}
