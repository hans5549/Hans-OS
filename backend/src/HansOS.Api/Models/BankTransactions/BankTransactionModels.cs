using System.ComponentModel.DataAnnotations;

using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.BankTransactions;

// Request DTOs
public record CreateBankTransactionRequest(
    [Required] TransactionType TransactionType,
    [Required] DateOnly TransactionDate,
    [Required][StringLength(200)] string Description,
    Guid? DepartmentId,
    [StringLength(100)] string? RequestingUnit,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Range(0, double.MaxValue)] decimal Fee = 0,
    bool HasReceipt = false,
    bool ReceiptMailed = false);

public record UpdateBankTransactionRequest(
    [Required] TransactionType TransactionType,
    [Required] DateOnly TransactionDate,
    [Required][StringLength(200)] string Description,
    Guid? DepartmentId,
    [StringLength(100)] string? RequestingUnit,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Range(0, double.MaxValue)] decimal Fee = 0,
    bool HasReceipt = false,
    bool ReceiptMailed = false);

// Response DTOs
public record BankTransactionResponse(
    Guid Id,
    string BankName,
    TransactionType TransactionType,
    DateOnly TransactionDate,
    string Description,
    Guid? DepartmentId,
    string? DepartmentName,
    string? RequestingUnit,
    decimal Amount,
    decimal Fee,
    bool HasReceipt,
    bool ReceiptMailed,
    decimal RunningBalance);

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
