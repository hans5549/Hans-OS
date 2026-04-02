using System.Security.Cryptography;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BudgetShare;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class BudgetShareService(
    ApplicationDbContext db,
    ILogger<BudgetShareService> logger) : IBudgetShareService
{
    // ── 管理端 ───────────────────────────────────

    /// <inheritdoc />
    public async Task<DepartmentShareInfoResponse> GetOrCreateShareTokenAsync(
        Guid departmentId, CancellationToken ct = default)
    {
        var dept = await FindDepartmentAsync(departmentId, ct);
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentId == dept.Id, ct);

        if (token is not null)
        {
            return ToShareInfoResponse(token);
        }

        var now = DateTime.UtcNow;
        token = new BudgetShareToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            DepartmentId = dept.Id,
            Permission = SharePermission.Editable,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.BudgetShareTokens.Add(token);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("自動建立部門分享 Token: DeptId={DeptId}", departmentId);
        return ToShareInfoResponse(token);
    }

    /// <inheritdoc />
    public async Task<DepartmentShareInfoResponse> UpdateShareAsync(
        Guid departmentId, UpdateShareRequest request, CancellationToken ct = default)
    {
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentId == departmentId, ct)
            ?? throw new KeyNotFoundException("此部門尚未建立分享連結");

        if (request.Permission is not null)
        {
            if (!Enum.TryParse<SharePermission>(request.Permission, ignoreCase: true, out var perm))
            {
                throw new ArgumentException("無效的權限值");
            }

            token.Permission = perm;
        }

        if (request.IsActive.HasValue)
        {
            token.IsActive = request.IsActive.Value;
        }

        token.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToShareInfoResponse(token);
    }

    /// <inheritdoc />
    public async Task<DepartmentShareInfoResponse> RegenerateTokenAsync(
        Guid departmentId, CancellationToken ct = default)
    {
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentId == departmentId, ct)
            ?? throw new KeyNotFoundException("此部門尚未建立分享連結");

        token.Token = GenerateToken();
        token.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("重新產生部門分享 Token: DeptId={DeptId}", departmentId);
        return ToShareInfoResponse(token);
    }

    /// <inheritdoc />
    public async Task RevokeShareAsync(Guid departmentId, CancellationToken ct = default)
    {
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentId == departmentId, ct)
            ?? throw new KeyNotFoundException("此部門尚未建立分享連結");

        db.BudgetShareTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("撤銷部門分享 Token: DeptId={DeptId}", departmentId);
    }

    // ── 公開端 ───────────────────────────────────

    /// <inheritdoc />
    public async Task<DepartmentShareOverviewResponse> GetDepartmentOverviewByTokenAsync(
        string token, CancellationToken ct = default)
    {
        var share = await ResolveTokenAsync(token, ct);

        var years = await db.DepartmentBudgets
            .AsNoTracking()
            .Where(d => d.DepartmentId == share.DepartmentId)
            .Include(d => d.AnnualBudget)
            .OrderByDescending(d => d.AnnualBudget.Year)
            .Select(d => new BudgetYearSummary(d.AnnualBudget.Year, d.AnnualBudget.Status.ToString()))
            .ToListAsync(ct);

        return new DepartmentShareOverviewResponse(
            share.Department.Name,
            share.Permission.ToString(),
            years);
    }

    /// <inheritdoc />
    public async Task<PublicBudgetResponse> GetBudgetByTokenAsync(
        string token, int year, CancellationToken ct = default)
    {
        var share = await ResolveTokenAsync(token, ct);

        var deptBudget = await db.DepartmentBudgets
            .AsNoTracking()
            .Include(d => d.AnnualBudget)
            .FirstOrDefaultAsync(d => d.DepartmentId == share.DepartmentId
                && d.AnnualBudget.Year == year, ct)
            ?? throw new KeyNotFoundException($"{year} 年度尚未建立預算");

        var budgetStatus = deptBudget.AnnualBudget.Status;

        var items = await db.BudgetItems
            .AsNoTracking()
            .Where(i => i.DepartmentBudgetId == deptBudget.Id)
            .OrderBy(i => i.Sequence)
            .Select(i => new PublicBudgetItemResponse(
                i.Id, i.Sequence, i.ActivityName, i.ContentItem, i.Amount, i.Note))
            .ToListAsync(ct);

        return new PublicBudgetResponse(
            share.Department.Name,
            year,
            ComputeEffectivePermission(share.Permission, budgetStatus, year),
            budgetStatus.ToString(),
            items);
    }

    /// <inheritdoc />
    public async Task<List<PublicBudgetItemResponse>> SaveItemsByTokenAsync(
        string token, int year, PublicSaveBudgetItemsRequest request, CancellationToken ct = default)
    {
        var share = await ResolveTokenAsync(token, ct);

        var deptBudget = await db.DepartmentBudgets
            .Include(d => d.AnnualBudget)
            .FirstOrDefaultAsync(d => d.DepartmentId == share.DepartmentId
                && d.AnnualBudget.Year == year, ct)
            ?? throw new KeyNotFoundException($"{year} 年度尚未建立預算");

        var budgetStatus = deptBudget.AnnualBudget.Status;
        var effective = ComputeEffectivePermission(share.Permission, budgetStatus, year);

        if (effective != nameof(SharePermission.Editable))
        {
            throw new UnauthorizedAccessException("目前為唯讀模式，無法儲存");
        }

        var deptBudgetId = deptBudget.Id;
        var existingItems = await db.BudgetItems
            .Where(i => i.DepartmentBudgetId == deptBudgetId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        RemoveDeletedItems(existingItems, request.Items);
        UpsertItems(existingItems, request.Items, deptBudgetId, now);

        deptBudget.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        return await db.BudgetItems
            .AsNoTracking()
            .Where(i => i.DepartmentBudgetId == deptBudgetId)
            .OrderBy(i => i.Sequence)
            .Select(i => new PublicBudgetItemResponse(
                i.Id, i.Sequence, i.ActivityName, i.ContentItem, i.Amount, i.Note))
            .ToListAsync(ct);
    }

    // ── Private helpers ──────────────────────────

    private async Task<BudgetShareToken> ResolveTokenAsync(string token, CancellationToken ct)
    {
        var share = await db.BudgetShareTokens
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Token == token, ct)
            ?? throw new KeyNotFoundException("分享連結不存在或已失效");

        if (!share.IsActive)
        {
            throw new UnauthorizedAccessException("此分享連結已停用");
        }

        return share;
    }

    private async Task<SportsDepartment> FindDepartmentAsync(Guid departmentId, CancellationToken ct)
        => await db.SportsDepartments
            .FirstOrDefaultAsync(d => d.Id == departmentId, ct)
            ?? throw new KeyNotFoundException($"找不到部門 {departmentId}");

    /// <summary>計算有效權限（考慮 Token 權限、預算狀態、是否為歷史年度）</summary>
    private static string ComputeEffectivePermission(
        SharePermission tokenPermission, BudgetStatus budgetStatus, int year)
    {
        // 歷史年度一律唯讀
        if (year < DateTime.UtcNow.Year)
        {
            return nameof(SharePermission.ReadOnly);
        }

        // 非 Draft 狀態一律唯讀
        if (budgetStatus > BudgetStatus.Draft)
        {
            return nameof(SharePermission.ReadOnly);
        }

        return tokenPermission.ToString();
    }

    private static DepartmentShareInfoResponse ToShareInfoResponse(BudgetShareToken token)
        => new(
            token.Id,
            token.Token,
            token.Permission.ToString(),
            token.IsActive,
            token.CreatedAt);

    private static string GenerateToken()
    {
        var bytes = new byte[48];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private void RemoveDeletedItems(
        IEnumerable<BudgetItem> existingItems,
        IEnumerable<PublicBudgetItemInput> inputItems)
    {
        var incomingIds = inputItems
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id!.Value)
            .ToHashSet();

        var toDelete = existingItems.Where(item => !incomingIds.Contains(item.Id)).ToList();
        db.BudgetItems.RemoveRange(toDelete);
    }

    private void UpsertItems(
        IEnumerable<BudgetItem> existingItems,
        IEnumerable<PublicBudgetItemInput> inputItems,
        Guid departmentBudgetId,
        DateTime now)
    {
        var existingById = existingItems.ToDictionary(item => item.Id);
        foreach (var input in inputItems)
        {
            if (!input.Id.HasValue)
            {
                db.BudgetItems.Add(new BudgetItem
                {
                    Id = Guid.NewGuid(),
                    DepartmentBudgetId = departmentBudgetId,
                    Sequence = input.Sequence,
                    ActivityName = input.ActivityName,
                    ContentItem = input.ContentItem,
                    Amount = input.Amount,
                    Note = input.Note,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                continue;
            }

            var existing = existingById.GetValueOrDefault(input.Id.Value)
                ?? throw new KeyNotFoundException($"預算項目 {input.Id.Value} 不存在");

            existing.Sequence = input.Sequence;
            existing.ActivityName = input.ActivityName;
            existing.ContentItem = input.ContentItem;
            existing.Amount = input.Amount;
            existing.Note = input.Note;
            existing.UpdatedAt = now;
        }
    }
}
