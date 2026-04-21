using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todo;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>待辦標籤管理 API</summary>
[ApiController]
[Route("todo/tags")]
[Authorize]
public class TodoTagController(ITodoTagService tagService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得所有標籤</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<TodoTagResponse>>> GetTags(CancellationToken ct)
        => ApiEnvelope<List<TodoTagResponse>>.Success(
            await tagService.GetTagsAsync(CurrentUserId, ct));

    /// <summary>新增標籤</summary>
    [HttpPost]
    public async Task<ApiEnvelope<TodoTagResponse>> CreateTag(
        [FromBody] CreateTodoTagRequest request, CancellationToken ct)
        => ApiEnvelope<TodoTagResponse>.Success(
            await tagService.CreateTagAsync(CurrentUserId, request, ct));

    /// <summary>更新標籤</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<TodoTagResponse>> UpdateTag(
        Guid id, [FromBody] UpdateTodoTagRequest request, CancellationToken ct)
        => ApiEnvelope<TodoTagResponse>.Success(
            await tagService.UpdateTagAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除標籤</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteTag(Guid id, CancellationToken ct)
    {
        await tagService.DeleteTagAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
