using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services;

public interface ITodoCategoryService
{
    Task<List<CategoryResponse>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(string userId, CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(string userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken ct = default);
}
