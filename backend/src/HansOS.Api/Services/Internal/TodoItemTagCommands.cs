using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemTagCommands
{
    public static void AddTags(ApplicationDbContext db, Guid itemId, IReadOnlyCollection<Guid>? tagIds)
    {
        if (tagIds is not { Count: > 0 }) return;

        var tags = tagIds
            .Select(tagId => new TodoItemTag { TodoItemId = itemId, TodoTagId = tagId });
        db.Set<TodoItemTag>().AddRange(tags);
    }

    public static async Task ReplaceTagsAsync(
        ApplicationDbContext db,
        Guid itemId,
        IReadOnlyCollection<Guid>? tagIds,
        CancellationToken ct)
    {
        if (tagIds is null) return;

        var existing = await db.Set<TodoItemTag>()
            .Where(t => t.TodoItemId == itemId)
            .ToListAsync(ct);
        db.Set<TodoItemTag>().RemoveRange(existing);

        AddTags(db, itemId, tagIds);
    }
}
