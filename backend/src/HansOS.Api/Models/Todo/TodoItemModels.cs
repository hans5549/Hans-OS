using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Todo;

// ── Request DTOs ──────────────────────────────────

public record CreateTodoItemRequest(
    [Required][StringLength(200)] string Title,
    [StringLength(10000)] string? Description = null,
    string? Status = null,
    string? Priority = null,
    string? Difficulty = null,
    DateOnly? ScheduledDate = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAt = null,
    Guid? ParentId = null,
    Guid? CategoryId = null,
    List<Guid>? TagIds = null,
    string? RecurrencePattern = null,
    int RecurrenceInterval = 1);

public record UpdateTodoItemRequest(
    [Required][StringLength(200)] string Title,
    [StringLength(10000)] string? Description = null,
    string? Priority = null,
    string? Difficulty = null,
    DateOnly? ScheduledDate = null,
    DateOnly? DueDate = null,
    DateTime? ReminderAt = null,
    Guid? ParentId = null,
    Guid? CategoryId = null,
    List<Guid>? TagIds = null,
    string? RecurrencePattern = null,
    int RecurrenceInterval = 1);

public record UpdateTodoStatusRequest(
    [Required] string Status);

public record ArchiveTodoRequest(
    bool Archive = true);

// ── Checklist ────────────────────────────────────

public record CreateChecklistItemRequest(
    [Required][StringLength(200)] string Title);

public record UpdateChecklistItemRequest(
    [StringLength(200)] string? Title = null,
    bool? IsCompleted = null);

public record SortChecklistRequest(
    [Required] List<Guid> OrderedIds);

// ── Batch ────────────────────────────────────────

public record BatchUpdateTodoRequest(
    [Required] List<Guid> Ids,
    string? Status = null,
    string? Priority = null,
    Guid? CategoryId = null,
    bool? Archive = null,
    bool? Delete = null);

// ── Sort ─────────────────────────────────────────

public record SortTodoRequest(
    [Required] List<Guid> OrderedIds,
    Guid? ParentId = null);

// ── Query Params ─────────────────────────────────

public class TodoQueryParams
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Difficulty { get; set; }
    public Guid? CategoryId { get; set; }
    public string? TagIds { get; set; }
    public Guid? ParentId { get; set; }
    public bool? TopLevelOnly { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public bool? Overdue { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "sortOrder";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ── Response DTOs ────────────────────────────────

public record TodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string Difficulty,
    DateOnly? ScheduledDate,
    DateOnly? DueDate,
    DateTime? ReminderAt,
    int SortOrder,
    string? RecurrencePattern,
    int RecurrenceInterval,
    Guid? RecurrenceSourceId,
    Guid? ParentId,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    List<TodoTagResponse> Tags,
    int ChildCount,
    int ChildCompletedCount,
    int ChecklistCount,
    int ChecklistCompletedCount,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TodoItemDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string Difficulty,
    DateOnly? ScheduledDate,
    DateOnly? DueDate,
    DateTime? ReminderAt,
    int SortOrder,
    string? RecurrencePattern,
    int RecurrenceInterval,
    Guid? RecurrenceSourceId,
    Guid? ParentId,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryColor,
    List<TodoTagResponse> Tags,
    List<TodoItemResponse> Children,
    List<ChecklistItemResponse> ChecklistItems,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ChecklistItemResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    int SortOrder);

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record TodoStatsResponse(
    int Total,
    int Pending,
    int InProgress,
    int Completed,
    int Overdue,
    int Archived);
