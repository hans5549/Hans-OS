using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todos;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>待辦事項標籤 API</summary>
[ApiController]
[Route("todo")]
[Authorize]
public class TodoTagController(ITodoTagService tagService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("tags")]
    public async Task<ApiEnvelope<List<TagResponse>>> GetAll(CancellationToken ct)
        => ApiEnvelope<List<TagResponse>>.Success(
            await tagService.GetAllAsync(CurrentUserId, ct));

    [HttpPost("tags")]
    public async Task<ApiEnvelope<TagResponse>> Create(
        [FromBody] CreateTagRequest request, CancellationToken ct)
        => ApiEnvelope<TagResponse>.Success(
            await tagService.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("tags/{id:guid}")]
    public async Task<ApiEnvelope<TagResponse>> Update(
        Guid id, [FromBody] UpdateTagRequest request, CancellationToken ct)
        => ApiEnvelope<TagResponse>.Success(
            await tagService.UpdateAsync(CurrentUserId, id, request, ct));

    [HttpDelete("tags/{id:guid}")]
    public async Task<ApiEnvelope<object?>> Delete(Guid id, CancellationToken ct)
    {
        await tagService.DeleteAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
