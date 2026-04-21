using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

public record CreateAccountRequest(
    [Required][StringLength(100)] string Name,
    [Required] string AccountType,
    [StringLength(3)] string Currency = "TWD",
    decimal InitialBalance = 0,
    string? Icon = null,
    int SortOrder = 0);

public record UpdateAccountRequest(
    [Required][StringLength(100)] string Name,
    [Required] string AccountType,
    [StringLength(3)] string Currency = "TWD",
    decimal InitialBalance = 0,
    string? Icon = null,
    int SortOrder = 0,
    bool IsArchived = false);

public record AccountResponse(
    Guid Id,
    string Name,
    string AccountType,
    string Currency,
    decimal InitialBalance,
    string? Icon,
    int SortOrder,
    bool IsArchived);

public record AccountBalanceResponse(
    Guid Id,
    string Name,
    string AccountType,
    string Currency,
    decimal InitialBalance,
    decimal CurrentBalance,
    string? Icon,
    bool IsArchived);
