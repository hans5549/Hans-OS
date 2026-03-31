using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.PendingRemittances;

using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class PendingRemittanceService(ApplicationDbContext db) : IPendingRemittanceService
{
    public async Task<List<PendingRemittanceResponse>> GetAllAsync(
        PendingRemittanceStatus? status = null, CancellationToken ct = default)
    {
        var query = db.PendingRemittances
            .AsNoTracking()
            .Include(r => r.Department)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapToResponse(r))
            .ToListAsync(ct);
    }

    public async Task<PendingRemittanceResponse> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var entity = await db.PendingRemittances
            .AsNoTracking()
            .Include(r => r.Department)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException("待匯款紀錄不存在");

        return MapToResponse(entity);
    }

    public async Task<PendingRemittanceResponse> CreateAsync(
        CreatePendingRemittanceRequest request, CancellationToken ct = default)
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
        var entity = new PendingRemittance
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            Amount = request.Amount,
            SourceAccount = request.SourceAccount.Trim(),
            TargetAccount = request.TargetAccount.Trim(),
            DepartmentId = request.DepartmentId,
            RecipientName = request.RecipientName?.Trim(),
            ExpectedDate = request.ExpectedDate,
            Note = request.Note?.Trim(),
            Status = PendingRemittanceStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.PendingRemittances.Add(entity);
        await db.SaveChangesAsync(ct);

        var department = entity.DepartmentId.HasValue
            ? await db.SportsDepartments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == entity.DepartmentId, ct)
            : null;

        return new PendingRemittanceResponse(
            entity.Id,
            entity.Description,
            entity.Amount,
            entity.SourceAccount,
            entity.TargetAccount,
            entity.DepartmentId,
            department?.Name,
            entity.RecipientName,
            entity.ExpectedDate,
            entity.Note,
            entity.Status,
            entity.CompletedAt,
            entity.CreatedAt);
    }

    public async Task UpdateAsync(
        Guid id, UpdatePendingRemittanceRequest request, CancellationToken ct = default)
    {
        var entity = await db.PendingRemittances.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("待匯款紀錄不存在");

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.SportsDepartments
                .AnyAsync(d => d.Id == request.DepartmentId.Value, ct);
            if (!departmentExists)
            {
                throw new ArgumentException("指定的部門不存在");
            }
        }

        entity.Description = request.Description.Trim();
        entity.Amount = request.Amount;
        entity.SourceAccount = request.SourceAccount.Trim();
        entity.TargetAccount = request.TargetAccount.Trim();
        entity.DepartmentId = request.DepartmentId;
        entity.RecipientName = request.RecipientName?.Trim();
        entity.ExpectedDate = request.ExpectedDate;
        entity.Note = request.Note?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.PendingRemittances.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("待匯款紀錄不存在");

        db.PendingRemittances.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.PendingRemittances.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("待匯款紀錄不存在");

        if (entity.Status == PendingRemittanceStatus.Completed)
        {
            throw new ArgumentException("此匯款紀錄已完成");
        }

        entity.Status = PendingRemittanceStatus.Completed;
        entity.CompletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    private static PendingRemittanceResponse MapToResponse(PendingRemittance r) =>
        new(r.Id,
            r.Description,
            r.Amount,
            r.SourceAccount,
            r.TargetAccount,
            r.DepartmentId,
            r.Department?.Name,
            r.RecipientName,
            r.ExpectedDate,
            r.Note,
            r.Status,
            r.CompletedAt,
            r.CreatedAt);
}
