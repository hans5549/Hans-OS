using System.ComponentModel.DataAnnotations;

using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.FinanceTasks;

/// <summary>新增財務任務請求</summary>
public record CreateFinanceTaskRequest(
    [Required][StringLength(200)] string Title,
    [StringLength(1000)] string? Description,
    [Required] FinanceTaskPriority Priority,
    DateOnly? DueDate,
    Guid? DepartmentId);

/// <summary>更新財務任務請求</summary>
public record UpdateFinanceTaskRequest(
    [Required][StringLength(200)] string Title,
    [StringLength(1000)] string? Description,
    [Required] FinanceTaskPriority Priority,
    DateOnly? DueDate,
    Guid? DepartmentId);

/// <summary>財務任務回應</summary>
public record FinanceTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    FinanceTaskPriority Priority,
    FinanceTaskStatus Status,
    DateOnly? DueDate,
    Guid? DepartmentId,
    string? DepartmentName,
    DateTime? CompletedAt,
    DateTime CreatedAt);
