using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Finance;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>交易分類管理 API</summary>
[ApiController]
[Route("finance/categories")]
[Authorize]
public class TransactionCategoryController(ITransactionCategoryService categoryService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>取得分類樹（含系統預設 + 使用者自訂）</summary>
    [HttpGet]
    public async Task<ApiEnvelope<List<CategoryResponse>>> GetCategories(
        [FromQuery] string? type, CancellationToken ct)
        => ApiEnvelope<List<CategoryResponse>>.Success(
            await categoryService.GetCategoriesAsync(CurrentUserId, type, ct));

    /// <summary>新增分類</summary>
    [HttpPost]
    public async Task<ApiEnvelope<CategoryResponse>> CreateCategory(
        [FromBody] CreateCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<CategoryResponse>.Success(
            await categoryService.CreateCategoryAsync(CurrentUserId, request, ct));

    /// <summary>更新分類</summary>
    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<CategoryResponse>> UpdateCategory(
        Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<CategoryResponse>.Success(
            await categoryService.UpdateCategoryAsync(CurrentUserId, id, request, ct));

    /// <summary>刪除分類</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> DeleteCategory(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteCategoryAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
