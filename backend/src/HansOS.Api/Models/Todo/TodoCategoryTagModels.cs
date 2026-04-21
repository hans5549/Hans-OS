using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Todo;

// ── TodoCategory ──────────────────────────────────

public record CreateTodoCategoryRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string? Color = null,
    [StringLength(200)] string? Icon = null,
    int SortOrder = 0);

public record UpdateTodoCategoryRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(20)] string? Color = null,
    [StringLength(200)] string? Icon = null,
    int SortOrder = 0);

public record TodoCategoryResponse(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int SortOrder,
    DateTime CreatedAt);

// ── TodoTag ───────────────────────────────────────

public record CreateTodoTagRequest(
    [Required][StringLength(50)] string Name,
    [StringLength(20)] string? Color = null);

public record UpdateTodoTagRequest(
    [Required][StringLength(50)] string Name,
    [StringLength(20)] string? Color = null);

public record TodoTagResponse(
    Guid Id,
    string Name,
    string? Color,
    DateTime CreatedAt);
