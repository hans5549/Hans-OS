using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

// Request for creating a finance account
public record CreateAccountRequest(
    [Required][StringLength(100)] string Name,
    [Required] string AccountType,  // Cash, Bank, CreditCard, EPayment, Investment
    decimal InitialBalance = 0,
    string? Icon = null,
    int SortOrder = 0);

// Request for updating a finance account
public record UpdateAccountRequest(
    [Required][StringLength(100)] string Name,
    [Required] string AccountType,
    decimal InitialBalance = 0,
    string? Icon = null,
    int SortOrder = 0,
    bool IsArchived = false);

// Response for a finance account
public record AccountResponse(
    Guid Id,
    string Name,
    string AccountType,
    decimal InitialBalance,
    string? Icon,
    int SortOrder,
    bool IsArchived);

// Response for account with calculated balance
public record AccountBalanceResponse(
    Guid Id,
    string Name,
    string AccountType,
    decimal InitialBalance,
    decimal CurrentBalance,
    string? Icon,
    bool IsArchived);
