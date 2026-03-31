using System.ComponentModel.DataAnnotations;

using HansOS.Api.Data.Entities;

namespace HansOS.Api.Models.PendingRemittances;

/// <summary>新增待匯款請求</summary>
public record CreatePendingRemittanceRequest(
    [Required][StringLength(200)] string Description,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required][StringLength(100)] string SourceAccount,
    [Required][StringLength(100)] string TargetAccount,
    Guid? DepartmentId,
    [StringLength(100)] string? RecipientName,
    DateOnly? ExpectedDate,
    [StringLength(1000)] string? Note);

/// <summary>更新待匯款請求</summary>
public record UpdatePendingRemittanceRequest(
    [Required][StringLength(200)] string Description,
    [Required][Range(0.01, double.MaxValue)] decimal Amount,
    [Required][StringLength(100)] string SourceAccount,
    [Required][StringLength(100)] string TargetAccount,
    Guid? DepartmentId,
    [StringLength(100)] string? RecipientName,
    DateOnly? ExpectedDate,
    [StringLength(1000)] string? Note);

/// <summary>待匯款回應</summary>
public record PendingRemittanceResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string SourceAccount,
    string TargetAccount,
    Guid? DepartmentId,
    string? DepartmentName,
    string? RecipientName,
    DateOnly? ExpectedDate,
    string? Note,
    PendingRemittanceStatus Status,
    DateTime? CompletedAt,
    DateTime CreatedAt);
