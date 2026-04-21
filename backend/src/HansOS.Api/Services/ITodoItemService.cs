using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services;

public interface ITodoItemService
{
    /// <summary>取得分頁任務列表</summary>
    Task<PagedItemsResponse> GetItemsAsync(PagedItemsQuery query, CancellationToken ct = default);

    /// <summary>取得任務詳情（含子任務、清單、標籤）</summary>
    Task<ItemDetailResponse?> GetItemDetailAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>取得各智慧視圖的待辦數量</summary>
    Task<TodoCountsResponse> GetCountsAsync(string userId, CancellationToken ct = default);

    /// <summary>建立任務</summary>
    Task<ItemResponse> CreateItemAsync(string userId, CreateItemRequest request, CancellationToken ct = default);

    /// <summary>更新任務</summary>
    Task<ItemResponse> UpdateItemAsync(string userId, Guid itemId, UpdateItemRequest request, CancellationToken ct = default);

    /// <summary>切換完成狀態（Done ↔ Pending）</summary>
    Task<ItemResponse> ToggleCompleteAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>更新任務狀態；若為週期任務完成則自動建立下一個</summary>
    Task<ItemDetailResponse> UpdateStatusAsync(string userId, Guid itemId, string status, CancellationToken ct = default);

    /// <summary>封存或取消封存任務</summary>
    Task<ItemDetailResponse> ArchiveAsync(string userId, Guid itemId, bool archive, CancellationToken ct = default);

    /// <summary>軟刪除任務</summary>
    Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>取得垃圾桶（軟刪除）任務</summary>
    Task<List<ItemResponse>> GetTrashAsync(string userId, CancellationToken ct = default);

    /// <summary>從垃圾桶還原任務</summary>
    Task RestoreAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>永久刪除任務</summary>
    Task PermanentDeleteAsync(string userId, Guid itemId, CancellationToken ct = default);

    /// <summary>新增清單子項目</summary>
    Task<ChecklistItemResponse> AddChecklistItemAsync(string userId, Guid itemId, CreateChecklistItemRequest request, CancellationToken ct = default);

    /// <summary>更新清單子項目</summary>
    Task<ChecklistItemResponse> UpdateChecklistItemAsync(string userId, Guid itemId, Guid checklistId, UpdateChecklistItemRequest request, CancellationToken ct = default);

    /// <summary>刪除清單子項目</summary>
    Task DeleteChecklistItemAsync(string userId, Guid itemId, Guid checklistId, CancellationToken ct = default);

    /// <summary>今日任務（dueDate 或 scheduledDate = 今天）</summary>
    Task<List<ItemResponse>> GetTodayAsync(string userId, CancellationToken ct = default);

    /// <summary>本週任務</summary>
    Task<List<ItemResponse>> GetWeekAsync(string userId, CancellationToken ct = default);

    /// <summary>本月任務</summary>
    Task<List<ItemResponse>> GetMonthAsync(string userId, CancellationToken ct = default);

    /// <summary>統計資訊</summary>
    Task<TodoStatsResponse> GetStatsAsync(string userId, CancellationToken ct = default);

    /// <summary>提醒數量（今日逾期 + 今日到期）</summary>
    Task<int> GetReminderCountAsync(string userId, CancellationToken ct = default);

    /// <summary>搜尋任務（標題模糊搜尋）</summary>
    Task<List<ItemResponse>> SearchAsync(string userId, string q, CancellationToken ct = default);

    /// <summary>批次更新狀態</summary>
    Task BatchUpdateAsync(string userId, List<Guid> ids, string status, CancellationToken ct = default);

    /// <summary>重新排序</summary>
    Task SortAsync(string userId, List<Guid> orderedIds, CancellationToken ct = default);
}
