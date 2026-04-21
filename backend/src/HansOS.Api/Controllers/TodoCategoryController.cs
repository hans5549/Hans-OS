using System.Security.Claims;
using HansOS.Api.Common;
using HansOS.Api.Models.Todos;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

/// <summary>待辦事項分類 API</summary>
[ApiController]
[Route("todo")]
[Authorize]
public class TodoCategoryController(ITodoCategoryService categoryService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("categories")]
    public async Task<ApiEnvelope<List<CategoryResponse>>> GetAll(CancellationToken ct)
        => ApiEnvelope<List<CategoryResponse>>.Success(
            await categoryService.GetAllAsync(CurrentUserId, ct));

    [HttpPost("categories")]
    public async Task<ApiEnvelope<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<CategoryResponse>.Success(
            await categoryService.CreateAsync(CurrentUserId, request, ct));

    [HttpPut("categories/{id:guid}")]
    public async Task<ApiEnvelope<CategoryResponse>> Update(
        Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        => ApiEnvelope<CategoryResponse>.Success(
            await categoryService.UpdateAsync(CurrentUserId, id, request, ct));

    [HttpDelete("categories/{id:guid}")]
    public async Task<ApiEnvelope<object?>> Delete(Guid id, CancellationToken ct)
    {
        await categoryService.DeleteAsync(CurrentUserId, id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
