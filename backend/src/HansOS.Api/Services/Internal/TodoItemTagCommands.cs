using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemTagCommands
{
    public static async Task AddTagsAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        IReadOnlyCollection<Guid>? tagIds,
        CancellationToken ct)
    {
        if (tagIds is not { Count: > 0 }) return;

        await ValidateTagOwnershipAsync(db, userId, tagIds, ct);

        var tags = tagIds
            .Select(tagId => new TodoItemTag { TodoItemId = itemId, TodoTagId = tagId });
        db.Set<TodoItemTag>().AddRange(tags);
    }

    public static async Task ReplaceTagsAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        IReadOnlyCollection<Guid>? tagIds,
        CancellationToken ct)
    {
        if (tagIds is null) return;

        var existing = await db.Set<TodoItemTag>()
            .Where(t => t.TodoItemId == itemId)
            .ToListAsync(ct);
        db.Set<TodoItemTag>().RemoveRange(existing);

        await AddTagsAsync(db, userId, itemId, tagIds, ct);
    }

    private static async Task ValidateTagOwnershipAsync(
        ApplicationDbContext db,
        string userId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken ct)
    {
        var distinctIds = tagIds.Distinct().ToList();
        var ownedCount = await db.TodoTags
            .AsNoTracking()
            .CountAsync(t => distinctIds.Contains(t.Id) && t.UserId == userId, ct);

        if (ownedCount != distinctIds.Count)
            throw new KeyNotFoundException("找不到指定的標籤");
    }
}
