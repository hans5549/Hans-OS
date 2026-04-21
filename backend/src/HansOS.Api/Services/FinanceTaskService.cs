using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.FinanceTasks;

using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class FinanceTaskService(ApplicationDbContext db) : IFinanceTaskService
{
    public async Task<List<FinanceTaskResponse>> GetAllAsync(
        FinanceTaskStatus? status = null, CancellationToken ct = default)
    {
        var query = db.FinanceTasks
            .AsNoTracking()
            .Include(t => t.Department)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => MapToResponse(t))
            .ToListAsync(ct);
    }

    public async Task<FinanceTaskResponse> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var entity = await db.FinanceTasks
            .AsNoTracking()
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new KeyNotFoundException("財務任務不存在");

        return MapToResponse(entity);
    }

    public async Task<FinanceTaskResponse> CreateAsync(
        CreateFinanceTaskRequest request, CancellationToken ct = default)
    {
        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.SportsDepartments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!departmentExists)
            {
                throw new ArgumentException("指定的部門不存在");
            }
        }

        var now = DateTime.UtcNow;
        var entity = new FinanceTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            Status = FinanceTaskStatus.Pending,
            DueDate = request.DueDate,
            DepartmentId = request.DepartmentId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.FinanceTasks.Add(entity);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task UpdateAsync(
        Guid id, UpdateFinanceTaskRequest request, CancellationToken ct = default)
    {
        var entity = await db.FinanceTasks.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("財務任務不存在");

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.SportsDepartments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!departmentExists)
            {
                throw new ArgumentException("指定的部門不存在");
            }
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.Priority = request.Priority;
        entity.DueDate = request.DueDate;
        entity.DepartmentId = request.DepartmentId;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.FinanceTasks.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("財務任務不存在");

        db.FinanceTasks.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.FinanceTasks.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("財務任務不存在");

        if (entity.Status == FinanceTaskStatus.Completed)
        {
            throw new ArgumentException("此任務已完成");
        }

        var now = DateTime.UtcNow;
        entity.Status = FinanceTaskStatus.Completed;
        entity.CompletedAt = now;
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
    }

    private static FinanceTaskResponse MapToResponse(FinanceTask t) =>
        new(t.Id,
            t.Title,
            t.Description,
            t.Priority,
            t.Status,
            t.DueDate,
            t.DepartmentId,
            t.Department?.Name,
            t.CompletedAt,
            t.CreatedAt);
}
