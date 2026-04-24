using HansOS.Api.Data.Entities;

namespace HansOS.Api.Services.Internal;

internal static class TodoItemStatusRules
{
    public static TodoPriority ParsePriority(string priority)
        => Enum.TryParse<TodoPriority>(priority, ignoreCase: true, out var p) ? p : TodoPriority.None;

    public static TodoStatus ParseStatus(string status)
        => Enum.TryParse<TodoStatus>(status, ignoreCase: true, out var s) ? s : TodoStatus.Pending;

    public static TodoStatus ParseStatusFromClient(string status)
        => status.ToLowerInvariant() switch
        {
            "completed" => TodoStatus.Done,
            "pending" => TodoStatus.Pending,
            "inprogress" => TodoStatus.InProgress,
            _ => Enum.TryParse<TodoStatus>(status, ignoreCase: true, out var s)
                ? s
                : throw new ArgumentException($"無效的狀態值：{status}"),
        };

    public static TodoDifficulty ParseDifficulty(string difficulty)
        => Enum.TryParse<TodoDifficulty>(difficulty, ignoreCase: true, out var d) ? d : TodoDifficulty.None;

    public static RecurrencePattern ParseRecurrencePattern(string pattern)
        => Enum.TryParse<RecurrencePattern>(pattern, ignoreCase: true, out var p) ? p : RecurrencePattern.None;

    public static void SetItemStatus(TodoItem item, TodoStatus status, DateTime utcNow)
    {
        if (status is TodoStatus.Done)
        {
            if (item.Status is TodoStatus.Done) return;
            item.Status = TodoStatus.Done;
            item.CompletedAt = utcNow;
            return;
        }

        item.Status = status;
        item.CompletedAt = null;
    }

    public static void ToggleCompletedState(TodoItem item, DateTime utcNow)
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
}
