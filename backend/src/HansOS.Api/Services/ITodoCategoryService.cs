using HansOS.Api.Models.Todo;

namespace HansOS.Api.Services;

public interface ITodoCategoryService
{
    /// <summary>取得使用者所有分類（按 SortOrder 排序）</summary>
    Task<List<TodoCategoryResponse>> GetCategoriesAsync(string userId, CancellationToken ct = default);

    /// <summary>建立分類</summary>
    Task<TodoCategoryResponse> CreateCategoryAsync(string userId, CreateTodoCategoryRequest request, CancellationToken ct = default);

    /// <summary>更新分類</summary>
    Task<TodoCategoryResponse> UpdateCategoryAsync(string userId, Guid categoryId, UpdateTodoCategoryRequest request, CancellationToken ct = default);

    /// <summary>刪除分類（有任務時拒絕）</summary>
    Task DeleteCategoryAsync(string userId, Guid categoryId, CancellationToken ct = default);
}
