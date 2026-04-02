using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

public record CreateTransactionRequest(
    [Required] string TransactionType,  // Expense, Income, Transfer
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required] DateOnly TransactionDate,
    Guid? CategoryId,
    [Required] Guid AccountId,
    Guid? ToAccountId,  // required for Transfer
    [StringLength(500)] string? Note = null);

public record UpdateTransactionRequest(
    [Required] string TransactionType,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required] DateOnly TransactionDate,
    Guid? CategoryId,
    [Required] Guid AccountId,
    Guid? ToAccountId,
    [StringLength(500)] string? Note = null);

public record TransactionResponse(
    Guid Id,
    string TransactionType,
    decimal Amount,
    DateOnly TransactionDate,
    string? Note,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryIcon,
    Guid AccountId,
    string AccountName,
    Guid? ToAccountId,
    string? ToAccountName);

/// <summary>Summary of a single day's transactions</summary>
public record DailyTransactionGroup(
    DateOnly Date,
    decimal DayIncome,
    decimal DayExpense,
    List<TransactionResponse> Transactions);

/// <summary>Monthly summary statistics</summary>
public record MonthlySummaryResponse(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);
