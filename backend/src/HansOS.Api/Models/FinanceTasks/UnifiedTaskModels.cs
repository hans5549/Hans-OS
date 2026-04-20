using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.FinanceTasks;

/// <summary>統一任務類型</summary>
public enum UnifiedTaskType
{
    /// <summary>一般財務任務</summary>
    General = 0,

    /// <summary>待匯款</summary>
    Remittance = 1,

    /// <summary>收據追蹤</summary>
    Receipt = 2,
}

/// <summary>統一任務項目</summary>
public record UnifiedTaskItem(
    Guid Id,
    string Title,
    string? Description,
    UnifiedTaskType Type,
    FinanceTaskPriority Priority,
    FinanceTaskStatus Status,
    DateOnly? DueDate,
    string? DepartmentName,
    DateTime CreatedAt,
    Guid? SourceId);

/// <summary>統一任務清單回應（含統計）</summary>
public record UnifiedTaskListResponse(
    List<UnifiedTaskItem> Tasks,
    int TotalCount,
    int PendingCount,
    int InProgressCount,
    int CompletedCount);
