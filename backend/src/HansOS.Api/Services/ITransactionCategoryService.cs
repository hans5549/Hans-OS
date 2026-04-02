using HansOS.Api.Models.Finance;

namespace HansOS.Api.Services;

public interface ITransactionCategoryService
{
    /// <summary>取得分類清單（含系統預設），可依類型篩選</summary>
    Task<List<CategoryResponse>> GetCategoriesAsync(string userId, string? type = null, CancellationToken ct = default);

    /// <summary>建立自訂分類</summary>
    Task<CategoryResponse> CreateCategoryAsync(string userId, CreateCategoryRequest request, CancellationToken ct = default);

    /// <summary>更新自訂分類</summary>
    Task<CategoryResponse> UpdateCategoryAsync(string userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken ct = default);

    /// <summary>刪除自訂分類</summary>
    Task DeleteCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default);
}
