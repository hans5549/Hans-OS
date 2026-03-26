using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.TsfSettings;

public record CreateDepartmentRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(500)] string? Note);

public record UpdateDepartmentRequest(
    [Required][StringLength(100)] string Name,
    [StringLength(500)] string? Note);

public record DepartmentResponse(
    Guid Id,
    string Name,
    string? Note);

public record UpdateBankInitialBalanceRequest(
    [Required] decimal InitialAmount);

public record BankInitialBalanceResponse(
    Guid Id,
    string BankName,
    decimal InitialAmount);
