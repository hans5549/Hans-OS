using HansOS.Api.Data.Entities;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemRecurrenceFactory
{
    public static TodoItem CreateNext(TodoItem item, DateTime utcNow)
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

        return new TodoItem
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
    }
}
