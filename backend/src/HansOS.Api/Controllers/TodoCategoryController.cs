using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todo;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>待辦分類管理 API</summary>
[ApiController]
[Route("todo/categories")]
[Authorize]
public class TodoCategoryController(ITodoCategoryService categoryService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得所有分類</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<TodoCategoryResponse>>> GetCategories(CancellationToken ct)
        => ApiEnvelope<List<TodoCategoryResponse>>.Success(
            await categoryService.GetCategoriesAsync(CurrentUserId, ct));

    /// <summary>新增分類</summary>
    [HttpPost]
    public async Task<ApiEnvelope<TodoCategoryResponse>> CreateCategory(
        [FromBody] CreateTodoCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<TodoCategoryResponse>.Success(
            await categoryService.CreateCategoryAsync(CurrentUserId, request, ct));

    /// <summary>更新分類</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<TodoCategoryResponse>> UpdateCategory(
        Guid id, [FromBody] UpdateTodoCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<TodoCategoryResponse>.Success(
            await categoryService.UpdateCategoryAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除分類</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteCategory(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteCategoryAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
