using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services;

public interface ITodoTagService
{
    Task<List<TagResponse>> GetAllAsync(string userId, CancellationToken ct = default);
    Task<TagResponse> CreateAsync(string userId, CreateTagRequest request, CancellationToken ct = default);
    Task<TagResponse> UpdateAsync(string userId, Guid id, UpdateTagRequest request, CancellationToken ct = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken ct = default);
}
