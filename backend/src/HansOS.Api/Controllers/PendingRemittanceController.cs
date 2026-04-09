using HansOS.Api.Common;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.PendingRemittances;
using HansOS.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("pending-remittances")]
[Authorize]
public class PendingRemittanceController(IPendingRemittanceService service) : ControllerBase
{
    [HttpGet]
    public async Task<ApiEnvelope<List<PendingRemittanceResponse>>> GetAll(
        [FromQuery] PendingRemittanceStatus? status, CancellationToken ct)
    {
        var result = await service.GetAllAsync(status, ct);
        return ApiEnvelope<List<PendingRemittanceResponse>>.Success(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiEnvelope<PendingRemittanceResponse>> GetById(
        Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return ApiEnvelope<PendingRemittanceResponse>.Success(result);
    }

    [HttpPost]
    public async Task<ApiEnvelope<PendingRemittanceResponse>> Create(
        [FromBody] CreatePendingRemittanceRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ApiEnvelope<PendingRemittanceResponse>.Success(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ApiEnvelope<object?>> Update(
        Guid id, [FromBody] UpdatePendingRemittanceRequest request, CancellationToken ct)
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
    public async Task<ApiEnvelope<object?>> Complete(
        Guid id, [FromBody] CompletePendingRemittanceRequest request, CancellationToken ct)
    {
        await service.CompleteAsync(id, request, ct);
        return ApiEnvelope<object?>.Success(null);
    }
}
