using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemMapper
{
    public static ItemResponse ToItemResponseFromEntity(TodoItem i)
        => new(
            i.Id,
            i.Title,
            i.Description,
            i.Priority.ToString(),
            i.Status.ToString(),
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

    public static ItemDetailResponse ToItemDetailResponse(TodoItem i)
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

    private static string MapStatusToClient(TodoStatus status)
        => status == TodoStatus.Done ? "Completed" : status.ToString();
}
