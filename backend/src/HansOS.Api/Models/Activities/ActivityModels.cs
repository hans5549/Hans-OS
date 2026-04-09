using System.ComponentModel.DataAnnotations;

using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.Activities;

// ── Response ──────────────────────────────────────

/// <summary>活動摘要（列表用）</summary>
public record ActivitySummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    int Year,
    int Month,
    int Sequence,
    decimal TotalAmount,
    int GroupCount,
    int ExpenseCount);

/// <summary>活動完整明細</summary>
public record ActivityDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    int Year,
    int Month,
    int Sequence,
    decimal TotalAmount,
    List<ActivityGroupResponse> Groups,
    List<ActivityExpenseResponse> UngroupedExpenses);

/// <summary>活動分組</summary>
public record ActivityGroupResponse(
    Guid Id,
    string Name,
    int Sequence,
    decimal SubTotal,
    List<ActivityExpenseResponse> Expenses);

/// <summary>開銷項目</summary>
public record ActivityExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string? Note,
    int Sequence,
    Guid? BudgetItemId,
    string? BudgetItemName,
    Guid? PendingRemittanceId,
    PendingRemittanceStatus? PendingRemittanceStatus);

/// <summary>各月活動統計</summary>
public record MonthSummaryResponse(
    int Month,
    int ActivityCount,
    decimal TotalAmount);

// ── Request ───────────────────────────────────────

/// <summary>新增活動</summary>
public record CreateActivityRequest(
    [Required] Guid DepartmentId,
    [Required][Range(2000, 2100)] int Year,
    [Required][Range(1, 12)] int Month,
    [Required][StringLength(200)] string Name,
    [StringLength(1000)] string? Description,
    List<ActivityGroupInput>? Groups,
    List<ActivityExpenseInput>? Expenses);

/// <summary>更新活動</summary>
public record UpdateActivityRequest(
    [Required][StringLength(200)] string Name,
    [StringLength(1000)] string? Description,
    [Range(1, 12)] int? Month,
    List<ActivityGroupInput>? Groups,
    List<ActivityExpenseInput>? Expenses);

/// <summary>活動分組輸入</summary>
public record ActivityGroupInput(
    Guid? Id,
    [Required][StringLength(200)] string Name,
    int Sequence,
    List<ActivityExpenseInput> Expenses);

/// <summary>開銷項目輸入</summary>
public record ActivityExpenseInput(
    Guid? Id,
    [Required][StringLength(200)] string Description,
    [Required][Range(0, (double)decimal.MaxValue)] decimal Amount,
    [StringLength(1000)] string? Note,
    int Sequence,
    Guid? BudgetItemId);
