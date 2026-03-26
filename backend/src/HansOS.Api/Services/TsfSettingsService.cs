using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.TsfSettings;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class TsfSettingsService(ApplicationDbContext db) : ITsfSettingsService
{
    public async Task<List<DepartmentResponse>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        return await db.SportsDepartments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.Note))
            .ToListAsync(ct);
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(
        CreateDepartmentRequest request, CancellationToken ct = default)
    {
        var exists = await db.SportsDepartments
            .AnyAsync(d => d.Name == request.Name, ct);

        if (exists)
        {
            throw new ArgumentException($"部門名稱「{request.Name}」已存在");
        }

        var entity = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.SportsDepartments.Add(entity);
        await db.SaveChangesAsync(ct);

        return new DepartmentResponse(entity.Id, entity.Name, entity.Note);
    }

    public async Task UpdateDepartmentAsync(
        Guid id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        var entity = await db.SportsDepartments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"部門 {id} 不存在");

        var duplicate = await db.SportsDepartments
            .AnyAsync(d => d.Name == request.Name && d.Id != id, ct);

        if (duplicate)
        {
            throw new ArgumentException($"部門名稱「{request.Name}」已存在");
        }

        entity.Name = request.Name;
        entity.Note = request.Note;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.SportsDepartments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"部門 {id} 不存在");

        db.SportsDepartments.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BankInitialBalanceResponse>> GetBankInitialBalancesAsync(
        CancellationToken ct = default)
    {
        return await db.BankInitialBalances
            .AsNoTracking()
            .OrderBy(b => b.BankName)
            .Select(b => new BankInitialBalanceResponse(b.Id, b.BankName, b.InitialAmount))
            .ToListAsync(ct);
    }

    public async Task UpdateBankInitialBalanceAsync(
        Guid id, UpdateBankInitialBalanceRequest request, CancellationToken ct = default)
    {
        var entity = await db.BankInitialBalances.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"銀行起始資料 {id} 不存在");

        entity.InitialAmount = request.InitialAmount;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
