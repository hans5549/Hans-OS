using HansOS.Api.Models.Todo;

namespace HansOS.Api.Services;

public interface ITodoTagService
{
    /// <summary>取得使用者所有標籤（按名稱排序）</summary>
    Task<List<TodoTagResponse>> GetTagsAsync(string userId, CancellationToken ct = default);

    /// <summary>建立標籤</summary>
    Task<TodoTagResponse> CreateTagAsync(string userId, CreateTodoTagRequest request, CancellationToken ct = default);

    /// <summary>更新標籤</summary>
    Task<TodoTagResponse> UpdateTagAsync(string userId, Guid tagId, UpdateTodoTagRequest request, CancellationToken ct = default);

    /// <summary>刪除標籤</summary>
    Task DeleteTagAsync(string userId, Guid tagId, CancellationToken ct = default);
}
