using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.BudgetShare;

// ── Response ──────────────────────────────────────

/// <summary>部門分享連結資訊（管理端）</summary>
public record DepartmentShareInfoResponse(
    Guid Id,
    string Token,
    string Permission,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>部門分享總覽（公開端 — 含可用年度清單）</summary>
public record DepartmentShareOverviewResponse(
    string DepartmentName,
    string Permission,
    List<BudgetYearSummary> AvailableYears);

/// <summary>年度預算摘要</summary>
public record BudgetYearSummary(int Year, string Status);

/// <summary>公開預算資料（公開端）</summary>
public record PublicBudgetResponse(
    string DepartmentName,
    int Year,
    string EffectivePermission,
    string BudgetStatus,
    List<PublicBudgetItemResponse> Items);

/// <summary>公開預算項目</summary>
public record PublicBudgetItemResponse(
    Guid Id,
    int Sequence,
    string ActivityName,
    string ContentItem,
    decimal Amount,
    string? Note);

// ── Request ───────────────────────────────────────

/// <summary>更新分享設定</summary>
public record UpdateShareRequest(
    string? Permission,
    bool? IsActive);

/// <summary>公開端批次儲存預算項目</summary>
public record PublicSaveBudgetItemsRequest(
    [Required] List<PublicBudgetItemInput> Items);

/// <summary>公開端預算項目輸入（僅預算欄位，不含實際核銷）</summary>
public record PublicBudgetItemInput(
    Guid? Id,
    int Sequence,
    [Required][StringLength(200)] string ActivityName,
    [Required][StringLength(200)] string ContentItem,
    [Range(0, (double)decimal.MaxValue)] decimal Amount,
    [StringLength(1000)] string? Note);
