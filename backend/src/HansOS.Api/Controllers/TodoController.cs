using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todos;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>待辦事項 API</summary>
[ApiController]
[Route("todo")]
[Authorize]
public class TodoController(
    ITodoProjectService projectService,
    ITodoItemService itemService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ──────────── Projects ────────────

    /// <summary>取得所有專案</summary>
    [HttpGet("projects")]
    public async Task<ApiEnvelope<List<ProjectResponse>>> GetProjects(CancellationToken ct)
        => ApiEnvelope<List<ProjectResponse>>.Success(
            await projectService.GetProjectsAsync(CurrentUserId, ct));

    /// <summary>建立專案</summary>
    [HttpPost("projects")]
    public async Task<ApiEnvelope<ProjectResponse>> CreateProject(
        [FromBody] CreateProjectRequest request, CancellationToken ct)
        => ApiEnvelope<ProjectResponse>.Success(
            await projectService.CreateProjectAsync(CurrentUserId, request, ct));

    /// <summary>更新專案</summary>
    [HttpPut("projects/{id:guid}")]
    public async Task<ApiEnvelope<ProjectResponse>> UpdateProject(
        Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct)
        => ApiEnvelope<ProjectResponse>.Success(
            await projectService.UpdateProjectAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除專案（含所有任務）</summary>
    [HttpDelete("projects/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteProject(Guid id, CancellationToken ct)
    {
        await projectService.DeleteProjectAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ──────────── Items — Static Routes (must come before {id:guid}) ────────────

    /// <summary>取得統計資訊</summary>
    [HttpGet("items/stats")]
    public async Task<ApiEnvelope<TodoStatsResponse>> GetStats(CancellationToken ct)
        => ApiEnvelope<TodoStatsResponse>.Success(
            await itemService.GetStatsAsync(CurrentUserId, ct));

    /// <summary>今日任務</summary>
    [HttpGet("items/today")]
    public async Task<ApiEnvelope<List<ItemResponse>>> GetToday(CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.GetTodayAsync(CurrentUserId, ct));

    /// <summary>本週任務</summary>
    [HttpGet("items/week")]
    public async Task<ApiEnvelope<List<ItemResponse>>> GetWeek(CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.GetWeekAsync(CurrentUserId, ct));

    /// <summary>本月任務</summary>
    [HttpGet("items/month")]
    public async Task<ApiEnvelope<List<ItemResponse>>> GetMonth(CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.GetMonthAsync(CurrentUserId, ct));

    /// <summary>垃圾桶（軟刪除）任務</summary>
    [HttpGet("items/trash")]
    public async Task<ApiEnvelope<List<ItemResponse>>> GetTrash(CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.GetTrashAsync(CurrentUserId, ct));

    /// <summary>提醒數量</summary>
    [HttpGet("items/reminder-count")]
    public async Task<ApiEnvelope<int>> GetReminderCount(CancellationToken ct)
        => ApiEnvelope<int>.Success(
            await itemService.GetReminderCountAsync(CurrentUserId, ct));

    /// <summary>搜尋任務</summary>
    [HttpGet("items/search")]
    public async Task<ApiEnvelope<List<ItemResponse>>> Search([FromQuery] string q, CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.SearchAsync(CurrentUserId, q ?? string.Empty, ct));

    /// <summary>批次更新狀態</summary>
    [HttpPut("items/batch")]
    public async Task<ApiEnvelope<object?>> BatchUpdate([FromBody] BatchUpdateRequest request, CancellationToken ct)
    {
        await itemService.BatchUpdateAsync(CurrentUserId, request.Ids, request.Status, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    /// <summary>重新排序</summary>
    [HttpPut("items/sort")]
    public async Task<ApiEnvelope<object?>> Sort([FromBody] SortRequest request, CancellationToken ct)
    {
        await itemService.SortAsync(CurrentUserId, request.OrderedIds, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ──────────── Items — List & Create ────────────

    /// <summary>取得分頁任務列表</summary>
    [HttpGet("items")]
    public async Task<ApiEnvelope<PagedItemsResponse>> GetItems(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? view,
        [FromQuery] Guid? projectId,
        [FromQuery] bool topLevelOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => ApiEnvelope<PagedItemsResponse>.Success(
            await itemService.GetItemsAsync(
                new PagedItemsQuery(CurrentUserId, status, priority, view, projectId, topLevelOnly, search, page, pageSize),
                ct));

    /// <summary>取得各視圖待辦數量</summary>
    [HttpGet("counts")]
    public async Task<ApiEnvelope<TodoCountsResponse>> GetCounts(CancellationToken ct)
        => ApiEnvelope<TodoCountsResponse>.Success(
            await itemService.GetCountsAsync(CurrentUserId, ct));

    /// <summary>建立任務</summary>
    [HttpPost("items")]
    public async Task<ApiEnvelope<ItemResponse>> CreateItem(
        [FromBody] CreateItemRequest request, CancellationToken ct)
        => ApiEnvelope<ItemResponse>.Success(
            await itemService.CreateItemAsync(CurrentUserId, request, ct));

    // ──────────── Items — By ID ────────────

    /// <summary>取得任務詳情</summary>
    [HttpGet("items/{id:guid}")]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken ct)
    {
        var detail = await itemService.GetItemDetailAsync(CurrentUserId, id, ct);
        if (detail is null) return NotFound();
        return Ok(ApiEnvelope<ItemDetailResponse>.Success(detail));
    }

    /// <summary>更新任務</summary>
    [HttpPut("items/{id:guid}")]
    public async Task<ApiEnvelope<ItemResponse>> UpdateItem(
        Guid id, [FromBody] UpdateItemRequest request, CancellationToken ct)
        => ApiEnvelope<ItemResponse>.Success(
            await itemService.UpdateItemAsync(CurrentUserId, id, request, ct));

    /// <summary>切換完成狀態</summary>
    [HttpPatch("items/{id:guid}/complete")]
    public async Task<ApiEnvelope<ItemResponse>> ToggleComplete(Guid id, CancellationToken ct)
        => ApiEnvelope<ItemResponse>.Success(
            await itemService.ToggleCompleteAsync(CurrentUserId, id, ct));

    /// <summary>更新任務狀態</summary>
    [HttpPut("items/{id:guid}/status")]
    public async Task<ApiEnvelope<ItemDetailResponse>> UpdateStatus(
        Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
        => ApiEnvelope<ItemDetailResponse>.Success(
            await itemService.UpdateStatusAsync(CurrentUserId, id, request.Status, ct));

    /// <summary>封存或取消封存任務</summary>
    [HttpPut("items/{id:guid}/archive")]
    public async Task<ApiEnvelope<ItemDetailResponse>> Archive(
        Guid id, [FromBody] ArchiveRequest request, CancellationToken ct)
    {
        var result = await itemService.ArchiveAsync(CurrentUserId, id, request.Archive, ct);
        return ApiEnvelope<ItemDetailResponse>.Success(result);
    }

    /// <summary>從垃圾桶還原任務</summary>
    [HttpPut("items/{id:guid}/restore")]
    public async Task<ApiEnvelope<object?>> Restore(Guid id, CancellationToken ct)
    {
        await itemService.RestoreAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    /// <summary>永久刪除任務</summary>
    [HttpDelete("items/{id:guid}/permanent")]
    public async Task<ApiEnvelope<object?>> PermanentDelete(Guid id, CancellationToken ct)
    {
        await itemService.PermanentDeleteAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    /// <summary>軟刪除任務</summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteItem(Guid id, CancellationToken ct)
    {
        await itemService.DeleteItemAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    // ──────────── Checklist ────────────

    /// <summary>新增清單子項目</summary>
    [HttpPost("items/{id:guid}/checklist")]
    public async Task<ApiEnvelope<ChecklistItemResponse>> AddChecklistItem(
        Guid id, [FromBody] CreateChecklistItemRequest request, CancellationToken ct)
        => ApiEnvelope<ChecklistItemResponse>.Success(
            await itemService.AddChecklistItemAsync(CurrentUserId, id, request, ct));

    /// <summary>更新清單子項目</summary>
    [HttpPut("items/{id:guid}/checklist/{checklistId:guid}")]
    public async Task<ApiEnvelope<ChecklistItemResponse>> UpdateChecklistItem(
        Guid id, Guid checklistId, [FromBody] UpdateChecklistItemRequest request, CancellationToken ct)
        => ApiEnvelope<ChecklistItemResponse>.Success(
            await itemService.UpdateChecklistItemAsync(CurrentUserId, id, checklistId, request, ct));

    /// <summary>刪除清單子項目</summary>
    [HttpDelete("items/{id:guid}/checklist/{checklistId:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteChecklistItem(
        Guid id, Guid checklistId, CancellationToken ct)
    {
        await itemService.DeleteChecklistItemAsync(CurrentUserId, id, checklistId, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
