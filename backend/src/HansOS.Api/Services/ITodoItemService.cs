using HansOS.Api.Models.Todo;

namespace HansOS.Api.Services;

public interface ITodoItemService
{
    // ── CRUD ────────────────────────────────────────
    Task<PagedResponse<TodoItemResponse>> GetItemsAsync(string userId, TodoQueryParams query, CancellationToken ct = default);
    Task<TodoItemDetailResponse> GetItemAsync(string userId, Guid itemId, CancellationToken ct = default);
    Task<TodoItemResponse> CreateItemAsync(string userId, CreateTodoItemRequest request, CancellationToken ct = default);
    Task<TodoItemResponse> UpdateItemAsync(string userId, Guid itemId, UpdateTodoItemRequest request, CancellationToken ct = default);
    Task DeleteItemAsync(string userId, Guid itemId, CancellationToken ct = default);

    // ── Status & Lifecycle ──────────────────────────
    Task<TodoItemResponse> UpdateStatusAsync(string userId, Guid itemId, UpdateTodoStatusRequest request, CancellationToken ct = default);
    Task<TodoItemResponse> ArchiveItemAsync(string userId, Guid itemId, ArchiveTodoRequest request, CancellationToken ct = default);
    Task RestoreItemAsync(string userId, Guid itemId, CancellationToken ct = default);
    Task PermanentDeleteAsync(string userId, Guid itemId, CancellationToken ct = default);

    // ── Time-Based Views ────────────────────────────
    Task<List<TodoItemResponse>> GetTodayItemsAsync(string userId, CancellationToken ct = default);
    Task<List<TodoItemResponse>> GetWeekItemsAsync(string userId, CancellationToken ct = default);
    Task<List<TodoItemResponse>> GetMonthItemsAsync(string userId, CancellationToken ct = default);

    // ── Stats & Badge ───────────────────────────────
    Task<TodoStatsResponse> GetStatsAsync(string userId, CancellationToken ct = default);
    Task<int> GetReminderCountAsync(string userId, CancellationToken ct = default);

    // ── Trash ───────────────────────────────────────
    Task<List<TodoItemResponse>> GetTrashAsync(string userId, CancellationToken ct = default);

    // ── Search ──────────────────────────────────────
    Task<List<TodoItemResponse>> SearchAsync(string userId, string query, CancellationToken ct = default);

    // ── Checklist ───────────────────────────────────
    Task<ChecklistItemResponse> AddChecklistItemAsync(string userId, Guid itemId, CreateChecklistItemRequest request, CancellationToken ct = default);
    Task<ChecklistItemResponse> UpdateChecklistItemAsync(string userId, Guid itemId, Guid checklistId, UpdateChecklistItemRequest request, CancellationToken ct = default);
    Task DeleteChecklistItemAsync(string userId, Guid itemId, Guid checklistId, CancellationToken ct = default);

    // ── Batch & Sort ────────────────────────────────
    Task BatchUpdateAsync(string userId, BatchUpdateTodoRequest request, CancellationToken ct = default);
    Task SortItemsAsync(string userId, SortTodoRequest request, CancellationToken ct = default);
}
