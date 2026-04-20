using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todo;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("todo/items")]
[Authorize]
public class TodoItemController(ITodoItemService todoItemService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── CRUD ────────────────────────────────────────

    [HttpGet]
    public async Task<ApiEnvelope<PagedResponse<TodoItemResponse>>> GetItems(
        [FromQuery] TodoQueryParams query, CancellationToken ct)
        => ApiEnvelope<PagedResponse<TodoItemResponse>>.Success(
            await todoItemService.GetItemsAsync(CurrentUserId, query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ApiEnvelope<TodoItemDetailResponse>> GetItem(Guid id, CancellationToken ct)
        => ApiEnvelope<TodoItemDetailResponse>.Success(
            await todoItemService.GetItemAsync(CurrentUserId, id, ct));

    [HttpPost]
    public async Task<ApiEnvelope<TodoItemResponse>> CreateItem(
        [FromBody] CreateTodoItemRequest request, CancellationToken ct)
        => ApiEnvelope<TodoItemResponse>.Success(
            await todoItemService.CreateItemAsync(CurrentUserId, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<TodoItemResponse>> UpdateItem(
        Guid id, [FromBody] UpdateTodoItemRequest request, CancellationToken ct)
        => ApiEnvelope<TodoItemResponse>.Success(
            await todoItemService.UpdateItemAsync(CurrentUserId, id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteItem(Guid id, CancellationToken ct)
    {
        await todoItemService.DeleteItemAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ── Status & Lifecycle ──────────────────────────

    [HttpPut("{id:guid}/status")]
    public async Task<ApiEnvelope<TodoItemResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateTodoStatusRequest request, CancellationToken ct)
        => ApiEnvelope<TodoItemResponse>.Success(
            await todoItemService.UpdateStatusAsync(CurrentUserId, id, request, ct));

    [HttpPut("{id:guid}/archive")]
    public async Task<ApiEnvelope<TodoItemResponse>> ArchiveItem(
        Guid id, [FromBody] ArchiveTodoRequest request, CancellationToken ct)
        => ApiEnvelope<TodoItemResponse>.Success(
            await todoItemService.ArchiveItemAsync(CurrentUserId, id, request, ct));

    [HttpPut("{id:guid}/restore")]
    public async Task<ApiEnvelope<object?>> RestoreItem(Guid id, CancellationToken ct)
    {
        await todoItemService.RestoreItemAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpDelete("{id:guid}/permanent")]
    public async Task<ApiEnvelope<object?>> PermanentDelete(Guid id, CancellationToken ct)
    {
        await todoItemService.PermanentDeleteAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ── Time-Based Views ────────────────────────────

    [HttpGet("today")]
    public async Task<ApiEnvelope<List<TodoItemResponse>>> GetToday(CancellationToken ct)
        => ApiEnvelope<List<TodoItemResponse>>.Success(
            await todoItemService.GetTodayItemsAsync(CurrentUserId, ct));

    [HttpGet("week")]
    public async Task<ApiEnvelope<List<TodoItemResponse>>> GetWeek(CancellationToken ct)
        => ApiEnvelope<List<TodoItemResponse>>.Success(
            await todoItemService.GetWeekItemsAsync(CurrentUserId, ct));

    [HttpGet("month")]
    public async Task<ApiEnvelope<List<TodoItemResponse>>> GetMonth(CancellationToken ct)
        => ApiEnvelope<List<TodoItemResponse>>.Success(
            await todoItemService.GetMonthItemsAsync(CurrentUserId, ct));

    // ── Stats & Badge ───────────────────────────────

    [HttpGet("stats")]
    public async Task<ApiEnvelope<TodoStatsResponse>> GetStats(CancellationToken ct)
        => ApiEnvelope<TodoStatsResponse>.Success(
            await todoItemService.GetStatsAsync(CurrentUserId, ct));

    [HttpGet("reminder-count")]
    public async Task<ApiEnvelope<int>> GetReminderCount(CancellationToken ct)
        => ApiEnvelope<int>.Success(
            await todoItemService.GetReminderCountAsync(CurrentUserId, ct));

    // ── Trash ───────────────────────────────────────

    [HttpGet("trash")]
    public async Task<ApiEnvelope<List<TodoItemResponse>>> GetTrash(CancellationToken ct)
        => ApiEnvelope<List<TodoItemResponse>>.Success(
            await todoItemService.GetTrashAsync(CurrentUserId, ct));

    // ── Search ──────────────────────────────────────

    [HttpGet("search")]
    public async Task<ApiEnvelope<List<TodoItemResponse>>> Search(
        [FromQuery] string q, CancellationToken ct)
        => ApiEnvelope<List<TodoItemResponse>>.Success(
            await todoItemService.SearchAsync(CurrentUserId, q, ct));

    // ── Checklist ───────────────────────────────────

    [HttpPost("{id:guid}/checklist")]
    public async Task<ApiEnvelope<ChecklistItemResponse>> AddChecklistItem(
        Guid id, [FromBody] CreateChecklistItemRequest request, CancellationToken ct)
        => ApiEnvelope<ChecklistItemResponse>.Success(
            await todoItemService.AddChecklistItemAsync(CurrentUserId, id, request, ct));

    [HttpPut("{id:guid}/checklist/{checklistId:guid}")]
    public async Task<ApiEnvelope<ChecklistItemResponse>> UpdateChecklistItem(
        Guid id, Guid checklistId, [FromBody] UpdateChecklistItemRequest request, CancellationToken ct)
        => ApiEnvelope<ChecklistItemResponse>.Success(
            await todoItemService.UpdateChecklistItemAsync(CurrentUserId, id, checklistId, request, ct));

    [HttpDelete("{id:guid}/checklist/{checklistId:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteChecklistItem(
        Guid id, Guid checklistId, CancellationToken ct)
    {
        await todoItemService.DeleteChecklistItemAsync(CurrentUserId, id, checklistId, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ── Batch & Sort ────────────────────────────────

    [HttpPut("batch")]
    public async Task<ApiEnvelope<object?>> BatchUpdate(
        [FromBody] BatchUpdateTodoRequest request, CancellationToken ct)
    {
        await todoItemService.BatchUpdateAsync(CurrentUserId, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpPut("sort")]
    public async Task<ApiEnvelope<object?>> SortItems(
        [FromBody] SortTodoRequest request, CancellationToken ct)
    {
        await todoItemService.SortItemsAsync(CurrentUserId, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
