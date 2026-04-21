using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.ArticleCollection;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("article-collection")]
[Authorize]
public class ArticleCollectionController(
    IArticleBookmarkService bookmarkService,
    IArticleBookmarkGroupService groupService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ApiEnvelope<ArticleBookmarkListResponse>> GetBookmarks(
        [FromQuery] ArticleBookmarkQueryRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkListResponse>.Success(
            await bookmarkService.GetBookmarksAsync(CurrentUserId, request, ct));

    [HttpPost]
    public async Task<ApiEnvelope<ArticleBookmarkResponse>> CreateBookmark(
        [FromBody] CreateArticleBookmarkRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkResponse>.Success(
            await bookmarkService.CreateBookmarkAsync(CurrentUserId, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<ArticleBookmarkResponse>> UpdateBookmark(
        Guid id,
        [FromBody] UpdateArticleBookmarkRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkResponse>.Success(
            await bookmarkService.UpdateBookmarkAsync(CurrentUserId, id, request, ct));

    [HttpPatch("{id:guid}/state")]
    public async Task<ApiEnvelope<ArticleBookmarkResponse>> PatchBookmarkState(
        Guid id,
        [FromBody] PatchArticleBookmarkStateRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkResponse>.Success(
            await bookmarkService.PatchBookmarkStateAsync(CurrentUserId, id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteBookmark(Guid id, CancellationToken ct)
    {
        await bookmarkService.DeleteBookmarkAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpGet("groups")]
    public async Task<ApiEnvelope<List<ArticleBookmarkGroupResponse>>> GetGroups(CancellationToken ct)
        => ApiEnvelope<List<ArticleBookmarkGroupResponse>>.Success(
            await groupService.GetGroupsAsync(CurrentUserId, ct));

    [HttpPost("groups")]
    public async Task<ApiEnvelope<ArticleBookmarkGroupResponse>> CreateGroup(
        [FromBody] CreateArticleBookmarkGroupRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkGroupResponse>.Success(
            await groupService.CreateGroupAsync(CurrentUserId, request, ct));

    [HttpPut("groups/{id:guid}")]
    public async Task<ApiEnvelope<ArticleBookmarkGroupResponse>> UpdateGroup(
        Guid id,
        [FromBody] UpdateArticleBookmarkGroupRequest request,
        CancellationToken ct)
        => ApiEnvelope<ArticleBookmarkGroupResponse>.Success(
            await groupService.UpdateGroupAsync(CurrentUserId, id, request, ct));

    [HttpDelete("groups/{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteGroup(Guid id, CancellationToken ct)
    {
        await groupService.DeleteGroupAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
