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

    // ──────────── Items ────────────

    /// <summary>取得任務列表（可依視圖或專案篩選）</summary>
    [HttpGet("items")]
    public async Task<ApiEnvelope<List<ItemResponse>>> GetItems(
        [FromQuery] string? view,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
        => ApiEnvelope<List<ItemResponse>>.Success(
            await itemService.GetItemsAsync(CurrentUserId, ParseViewFilter(view), projectId, ct));

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

    /// <summary>刪除任務</summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteItem(Guid id, CancellationToken ct)
    {
        await itemService.DeleteItemAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    private static TodoViewFilter? ParseViewFilter(string? view)
        => view?.ToLowerInvariant() switch
        {
            "inbox" => TodoViewFilter.Inbox,
            "today" => TodoViewFilter.Today,
            "upcoming" => TodoViewFilter.Upcoming,
            "all" => TodoViewFilter.All,
            _ => null,
        };
}
