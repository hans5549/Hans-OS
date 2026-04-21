using HansOS.Api.Common;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.FinanceTasks;
using HansOS.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("finance-tasks")]
[Authorize]
public class FinanceTaskController(
    IFinanceTaskService service,
    IUnifiedTaskService unifiedService) : ControllerBase
{
    [HttpGet("unified")]
    public async Task<ApiEnvelope<UnifiedTaskListResponse>> GetUnified(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] FinanceTaskStatus? status,
        [FromQuery] UnifiedTaskType? type,
        CancellationToken ct)
    {
        var result = await unifiedService.GetUnifiedTasksAsync(year, month, status, type, ct);
        return ApiEnvelope<UnifiedTaskListResponse>.Success(result);
    }

    [HttpGet]
    public async Task<ApiEnvelope<List<FinanceTaskResponse>>> GetAll(
        [FromQuery] FinanceTaskStatus? status, CancellationToken ct)
    {
        var result = await service.GetAllAsync(status, ct);
        return ApiEnvelope<List<FinanceTaskResponse>>.Success(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiEnvelope<FinanceTaskResponse>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return ApiEnvelope<FinanceTaskResponse>.Success(result);
    }

    [HttpPost]
    public async Task<ApiEnvelope<FinanceTaskResponse>> Create(
        [FromBody] CreateFinanceTaskRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ApiEnvelope<FinanceTaskResponse>.Success(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<object?>> Update(
        Guid id, [FromBody] UpdateFinanceTaskRequest request, CancellationToken ct)
    {
        await service.UpdateAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ApiEnvelope<object?>> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiEnvelope<object?>.Success(null);
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<ApiEnvelope<object?>> Complete(Guid id, CancellationToken ct)
    {
        await service.CompleteAsync(id, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
