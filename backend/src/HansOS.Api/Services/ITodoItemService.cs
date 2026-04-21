using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services;

public interface ITodoItemService
{
    /// <summary>取得任務列表（依視圖篩選或專案篩選）</summary>
    Task<List<ItemResponse>> GetItemsAsync(
        string userId,
        TodoViewFilter? view = null,
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>取得各智慧視圖的待辦數量</summary>
    Task<TodoCountsResponse> GetCountsAsync(string userId, CancellationToken ct = default);

    /// <summary>建立任務</summary>
    Task<ItemResponse> CreateItemAsync(string userId, CreateItemRequest request, CancellationToken ct = default);

    /// <summary>更新任務</summary>
    Task<ItemResponse> UpdateItemAsync(string userId, Guid itemId, UpdateItemRequest request, CancellationToken ct = default);

    /// <summary>切換完成狀態（Done ↔ Pending）</summary>
    Task<ItemResponse> ToggleCompleteAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>刪除任務</summary>
    Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default);
}
