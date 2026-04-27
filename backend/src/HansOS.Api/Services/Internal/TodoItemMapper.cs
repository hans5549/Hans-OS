using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemMapper
{
    public static ItemResponse ToItemResponseFromEntity(TodoItem i)
        => ToItemResponse(i, ToChildResponses(i));

    public static ItemDetailResponse ToItemDetailResponse(TodoItem i)
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
            i.CategoryId,
            i.Category?.Name,
            i.Order,
            i.RecurrencePattern.ToString(),
            i.RecurrenceInterval,
            i.CreatedAt,
            i.CompletedAt,
            i.ArchivedAt,
            ToChildResponses(i),
            ToTagResponses(i));

    private static ItemResponse ToItemResponseWithoutChildren(TodoItem i)
        => ToItemResponse(i, []);

    private static ItemResponse ToItemResponse(TodoItem i, List<ItemResponse> children)
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
            i.CategoryId,
            i.Order,
            i.RecurrencePattern.ToString(),
            i.RecurrenceInterval,
            i.CreatedAt,
            i.CompletedAt,
            i.ArchivedAt,
            ToTagResponses(i),
            children);

    private static List<ItemResponse> ToChildResponses(TodoItem i)
        => i.Children
            .Where(c => c.UserId == i.UserId && c.DeletedAt is null && c.ArchivedAt is null)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.CreatedAt)
            .Select(ToItemResponseWithoutChildren)
            .ToList();

    private static List<TagResponse> ToTagResponses(TodoItem i)
        => i.TodoItemTags
            .Select(t => new TagResponse(t.TodoTag.Id, t.TodoTag.Name, t.TodoTag.Color))
            .ToList();
}
