using System.ComponentModel.DataAnnotations;

namespace HansOS.Api.Models.Finance;

public record CreateCategoryRequest(
    [Required][StringLength(100)] string Name,
    [Required] string CategoryType,  // Expense, Income
    Guid? ParentId = null,
    string? Icon = null,
    int SortOrder = 0);

public record UpdateCategoryRequest(
    [Required][StringLength(100)] string Name,
    string? Icon = null,
    int SortOrder = 0);

public record CategoryResponse(
    Guid Id,
    string Name,
    string CategoryType,
    string? Icon,
    int SortOrder,
    bool IsSystem,  // true if UserId is null (system default)
    List<CategoryResponse>? Children);
