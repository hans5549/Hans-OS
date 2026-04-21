using System.ComponentModel.DataAnnotations;

using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.BankTransactions;

// Request DTOs
public record CreateBankTransactionRequest(
    [Required] TransactionType TransactionType,
    [Required] DateOnly TransactionDate,
    [Required][StringLength(200)] string Description,
    Guid? DepartmentId,
    [Required][Range(1, double.MaxValue)] decimal Amount,
    [Range(0, double.MaxValue)] decimal Fee = 0,
    bool ReceiptCollected = false,
    bool ReceiptMailed = false,
    Guid? ActivityId = null);

public record UpdateBankTransactionRequest(
    [Required] TransactionType TransactionType,
    [Required] DateOnly TransactionDate,
    [Required][StringLength(200)] string Description,
    Guid? DepartmentId,
    [Required][Range(1, double.MaxValue)] decimal Amount,
    [Range(0, double.MaxValue)] decimal Fee = 0,
    bool ReceiptCollected = false,
    bool ReceiptMailed = false,
    Guid? ActivityId = null);

/// <summary>批次更新歸屬部門</summary>
public record BatchUpdateDepartmentRequest(
    [Required][StringLength(100)] string BankName,
    [Required][MinLength(1)][MaxLength(200)] List<Guid> Ids,
    [Range(2000, 2100)] int Year,
    [Range(1, 12)] int? Month,
    Guid? DepartmentId);

/// <summary>更新收據狀態請求</summary>
public record PatchReceiptStatusRequest(
    bool? ReceiptCollected,
    bool? ReceiptMailed);

// Response DTOs
public record BankTransactionResponse(
    Guid Id,
    string BankName,
    TransactionType TransactionType,
    DateOnly TransactionDate,
    string Description,
    Guid? DepartmentId,
    string? DepartmentName,
    decimal Amount,
    decimal Fee,
    bool ReceiptCollected,
    bool ReceiptMailed,
    decimal RunningBalance,
    Guid? ActivityId,
    string? ActivityName,
    Guid? PendingRemittanceId);

/// <summary>收據追蹤回應</summary>
public record ReceiptTrackingResponse(
    Guid Id,
    string BankName,
    DateOnly TransactionDate,
    string Description,
    Guid? DepartmentId,
    string? DepartmentName,
    decimal Amount,
    bool ReceiptCollected,
    bool ReceiptMailed);

/// <summary>收據追蹤統計摘要</summary>
public record ReceiptTrackingSummaryResponse(
    int TotalCount,
    int NotCollectedCount,
    int NotMailedCount,
    List<ReceiptTrackingResponse> Items);

public record BankTransactionSummaryResponse(
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance);

// Import result DTOs
public record ImportResultResponse(
    int TotalTransactions,
    List<BankImportDetail> Banks);

public record BankImportDetail(
    string BankName,
    int TransactionCount,
    decimal InitialBalance);
