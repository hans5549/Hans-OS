using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Todos;

// ──────────── Project ────────────

public record CreateProjectRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string Color = "#3B82F6");

public record UpdateProjectRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string Color,
    bool IsArchived = false);

public record ProjectResponse(
    Guid Id,
    string Name,
    string Color,
    int Order,
    bool IsArchived,
    int ItemCount);

// ──────────── Item ────────────

/// <summary>智慧視圖篩選</summary>
public enum TodoViewFilter
{
    Inbox,
    Today,
    Upcoming,
    All,
}

public record CreateItemRequest(
    [Required][StringLength(500)] string Title,
    [StringLength(2000)] string? Description = null,
    string Priority = "None",
    string Difficulty = "None",
    DateOnly? DueDate = null,
    DateOnly? ScheduledDate = null,
    Guid? ProjectId = null,
    Guid? ParentId = null,
    Guid? CategoryId = null,
    List<Guid>? TagIds = null,
    string RecurrencePattern = "None",
    int RecurrenceInterval = 1);

public record UpdateItemRequest(
    [Required][StringLength(500)] string Title,
    [StringLength(2000)] string? Description = null,
    string Priority = "None",
    string Difficulty = "None",
    string Status = "Pending",
    DateOnly? DueDate = null,
    DateOnly? ScheduledDate = null,
    Guid? ProjectId = null,
    Guid? ParentId = null,
    Guid? CategoryId = null,
    List<Guid>? TagIds = null,
    string RecurrencePattern = "None",
    int RecurrenceInterval = 1);

public record ItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    string Difficulty,
    DateOnly? DueDate,
    DateOnly? ScheduledDate,
    Guid? ProjectId,
    string? ProjectName,
    string? ProjectColor,
    Guid? ParentId,
    int Order,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    List<TagResponse> Tags);

public record ItemDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    string Difficulty,
    DateOnly? DueDate,
    DateOnly? ScheduledDate,
    Guid? ProjectId,
    string? ProjectName,
    string? ProjectColor,
    Guid? ParentId,
    Guid? CategoryId,
    string? CategoryName,
    int Order,
    string RecurrencePattern,
    int RecurrenceInterval,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    List<ItemResponse> Children,
    List<ChecklistItemResponse> ChecklistItems,
    List<TagResponse> Tags);

public record PagedItemsResponse(
    List<ItemResponse> Items,
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

public record TodoCountsResponse(
    int Inbox,
    int Today,
    int Upcoming,
    int All);

// ──────────── Item Requests ────────────

public record UpdateStatusRequest(
    [Required] string Status);

public record ArchiveRequest(bool Archive);

public record BatchUpdateRequest(
    [Required] List<Guid> Ids,
    [Required] string Status);

public record SortRequest(
    [Required] List<Guid> OrderedIds);

// ──────────── Category ────────────

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int SortOrder);

public record CreateCategoryRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string? Color = null,
    [StringLength(100)] string? Icon = null,
    int SortOrder = 0);

public record UpdateCategoryRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string? Color = null,
    [StringLength(100)] string? Icon = null,
    int SortOrder = 0);

// ──────────── Tag ────────────

public record TagResponse(
    Guid Id,
    string Name,
    string? Color);

public record CreateTagRequest(
    [Required][StringLength(50)] string Name,
    [StringLength(20)] string? Color = null);

public record UpdateTagRequest(
    [Required][StringLength(50)] string Name,
    [StringLength(20)] string? Color = null);

// ──────────── Checklist ────────────

public record ChecklistItemResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    int Order);

public record CreateChecklistItemRequest(
    [Required][StringLength(500)] string Title,
    int Order = 0);

public record UpdateChecklistItemRequest(
    [StringLength(500)] string? Title = null,
    bool? IsCompleted = null,
    int? Order = null);

// ──────────── Query ────────────

public record PagedItemsQuery(
    string UserId,
    string? Status = null,
    string? Priority = null,
    string? View = null,
    Guid? ProjectId = null,
    bool TopLevelOnly = false,
    string? Search = null,
    int Page = 1,
    int PageSize = 50);
