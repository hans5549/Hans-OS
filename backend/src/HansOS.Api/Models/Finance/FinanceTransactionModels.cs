using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

public record CreateTransactionRequest(
    [Required] string TransactionType,  // Expense, Income, Transfer, BalanceAdjustment, Interest
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required] DateOnly TransactionDate,
    Guid? CategoryId,
    [Required] Guid AccountId,
    Guid? ToAccountId,
    [StringLength(3)] string Currency = "TWD",
    [StringLength(100)] string? Project = null,
    string? Tags = null,
    [StringLength(500)] string? Note = null);

public record UpdateTransactionRequest(
    [Required] string TransactionType,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required] DateOnly TransactionDate,
    Guid? CategoryId,
    [Required] Guid AccountId,
    Guid? ToAccountId,
    [StringLength(3)] string Currency = "TWD",
    [StringLength(100)] string? Project = null,
    string? Tags = null,
    [StringLength(500)] string? Note = null);

public record TransactionResponse(
    Guid Id,
    string TransactionType,
    decimal Amount,
    DateOnly TransactionDate,
    string? Note,
    string Currency,
    string? Project,
    List<string>? Tags,
    Guid? CategoryId,
    string? CategoryName,
    string? CategoryIcon,
    Guid AccountId,
    string AccountName,
    Guid? ToAccountId,
    string? ToAccountName);

/// <summary>單日交易分組摘要</summary>
public record DailyTransactionGroup(
    DateOnly Date,
    decimal DayIncome,
    decimal DayExpense,
    List<TransactionResponse> Transactions);

/// <summary>月度摘要統計</summary>
public record MonthlySummaryResponse(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);

/// <summary>月度趨勢資料點</summary>
public record MonthlyTrendPoint(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);

/// <summary>跨月趨勢回應</summary>
public record TrendResponse(List<MonthlyTrendPoint> Months);

/// <summary>分類佔比項目</summary>
public record CategoryBreakdownItem(
    Guid CategoryId,
    string CategoryName,
    string? CategoryIcon,
    decimal Amount,
    decimal Percentage,
    int TransactionCount);

/// <summary>分類佔比回應</summary>
public record CategoryBreakdownResponse(
    int Year,
    int Month,
    string Type,
    decimal Total,
    List<CategoryBreakdownItem> Items);
