using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services.Internal;

internal static class TodoChecklistCommands
{
    public static async Task<ChecklistItemResponse> AddAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        CreateChecklistItemRequest request,
        CancellationToken ct)
    {
        _ = await TodoItemQueries.GetUserItemAsync(db, userId, itemId, ct);

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
        return ToResponse(checklist);
    }

    public static async Task<ChecklistItemResponse> UpdateAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        Guid checklistId,
        UpdateChecklistItemRequest request,
        CancellationToken ct)
    {
        _ = await TodoItemQueries.GetUserItemAsync(db, userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("找不到指定的清單項目");

        if (request.Title is not null) checklist.Title = request.Title;
        if (request.IsCompleted.HasValue) checklist.IsCompleted = request.IsCompleted.Value;
        if (request.Order.HasValue) checklist.Order = request.Order.Value;
        checklist.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ToResponse(checklist);
    }

    public static async Task DeleteAsync(
        ApplicationDbContext db,
        string userId,
        Guid itemId,
        Guid checklistId,
        CancellationToken ct)
    {
        _ = await TodoItemQueries.GetUserItemAsync(db, userId, itemId, ct);

        var checklist = await db.TodoChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TodoItemId == itemId, ct)
            ?? throw new KeyNotFoundException("找不到指定的清單項目");

        db.TodoChecklistItems.Remove(checklist);
        await db.SaveChangesAsync(ct);
    }

    private static ChecklistItemResponse ToResponse(TodoChecklistItem checklist)
        => new(checklist.Id, checklist.Title, checklist.IsCompleted, checklist.Order);
}
