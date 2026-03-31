using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.AnnualBudget;

// ── Request ───────────────────────────────────────

/// <summary>批量匯入預算項目</summary>
public record BudgetImportRequest(
    [Required] List<DepartmentImportInput> Departments);

/// <summary>部門匯入輸入</summary>
public record DepartmentImportInput(
    [Required][StringLength(100)] string DepartmentName,
    [Required] List<BudgetItemImportInput> Items);

/// <summary>匯入用預算項目輸入</summary>
public record BudgetItemImportInput(
    int Sequence,
    [Required][StringLength(200)] string ActivityName,
    [Required][StringLength(200)] string ContentItem,
    [Range(0, (double)decimal.MaxValue)] decimal Amount,
    [StringLength(1000)] string? Note);

// ── Response (匯入結果) ───────────────────────────

/// <summary>批量匯入結果</summary>
public record BudgetImportResultResponse(
    int Year,
    int TotalDepartments,
    int TotalItems,
    decimal TotalAmount,
    List<DepartmentImportSummary> Departments);

/// <summary>部門匯入摘要</summary>
public record DepartmentImportSummary(
    string DepartmentName,
    Guid DepartmentId,
    bool IsNewDepartment,
    int ItemCount,
    decimal TotalAmount);

// ── Response (預覽) ───────────────────────────────

/// <summary>批量匯入預覽</summary>
public record BudgetImportPreviewResponse(
    int Year,
    int TotalDepartments,
    int TotalItems,
    decimal TotalAmount,
    List<DepartmentImportPreview> Departments,
    List<string> Warnings);

/// <summary>部門匯入預覽</summary>
public record DepartmentImportPreview(
    string DepartmentName,
    bool IsNewDepartment,
    int ExistingItemCount,
    int NewItemCount,
    decimal TotalAmount,
    List<BudgetItemPreview> Items);

/// <summary>預算項目預覽</summary>
public record BudgetItemPreview(
    int Sequence,
    string ActivityName,
    string ContentItem,
    decimal Amount,
    string? Note);
