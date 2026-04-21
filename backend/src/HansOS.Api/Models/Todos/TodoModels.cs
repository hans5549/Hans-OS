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
    string Priority = "None",   // None, Low, Medium, High
    DateOnly? DueDate = null,
    Guid? ProjectId = null);

public record UpdateItemRequest(
    [Required][StringLength(500)] string Title,
    [StringLength(2000)] string? Description = null,
    string Priority = "None",
    string Status = "Pending",  // Pending, InProgress, Done
    DateOnly? DueDate = null,
    Guid? ProjectId = null);

public record ItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    DateOnly? DueDate,
    Guid? ProjectId,
    string? ProjectName,
    string? ProjectColor,
    int Order,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record TodoCountsResponse(
    int Inbox,
    int Today,
    int Upcoming,
    int All);
