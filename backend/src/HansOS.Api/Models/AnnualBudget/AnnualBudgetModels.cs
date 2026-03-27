using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.AnnualBudget;

// ── Response ──────────────────────────────────────

/// <summary>年度預算總覽</summary>
public record AnnualBudgetOverviewResponse(
    Guid Id,
    int Year,
    string Status,
    string? Note,
    decimal TotalBudget,
    decimal TotalActual,
    decimal? GrantedBudget,
    List<DepartmentBudgetSummaryResponse> Departments);

/// <summary>部門預算摘要</summary>
public record DepartmentBudgetSummaryResponse(
    Guid DepartmentBudgetId,
    Guid DepartmentId,
    string DepartmentName,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal? AllocatedAmount,
    int ItemCount);

/// <summary>預算項目</summary>
public record BudgetItemResponse(
    Guid Id,
    int Sequence,
    string ActivityName,
    string ContentItem,
    decimal Amount,
    string? Note,
    decimal? ActualAmount,
    string? ActualNote);

// ── Request ───────────────────────────────────────

/// <summary>批次儲存預算項目</summary>
public record SaveBudgetItemsRequest(
    [Required] List<BudgetItemInput> Items);

/// <summary>預算項目輸入</summary>
public record BudgetItemInput(
    Guid? Id,
    int Sequence,
    [Required][StringLength(200)] string ActivityName,
    [Required][StringLength(200)] string ContentItem,
    [Range(0, (double)decimal.MaxValue)] decimal Amount,
    [StringLength(1000)] string? Note,
    [Range(0, (double)decimal.MaxValue)] decimal? ActualAmount,
    [StringLength(1000)] string? ActualNote);

/// <summary>更新預算狀態</summary>
public record UpdateBudgetStatusRequest(
    [Required] string Status);

/// <summary>更新核定總預算</summary>
public record UpdateGrantedBudgetRequest(
    [Required][Range(0, (double)decimal.MaxValue)] decimal GrantedBudget);
