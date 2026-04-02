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

    public async Task<BudgetShareInfoResponse> CreateShareTokenAsync(
        int year, Guid departmentId, CancellationToken ct = default)
    {
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        var existing = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentBudgetId == deptBudget.Id, ct);

        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            existing.Token = GenerateToken();
            existing.UpdatedAt = now;
            logger.LogInformation("重新產生分享 Token: Year={Year}, DeptId={DeptId}", year, departmentId);
        }
        else
        {
            existing = new BudgetShareToken
            {
                Id = Guid.NewGuid(),
                Token = GenerateToken(),
                DepartmentBudgetId = deptBudget.Id,
                Permission = SharePermission.Editable,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.BudgetShareTokens.Add(existing);
            logger.LogInformation("建立分享 Token: Year={Year}, DeptId={DeptId}", year, departmentId);
        }

        await db.SaveChangesAsync(ct);

        var budgetStatus = await GetBudgetStatusAsync(deptBudget.AnnualBudgetId, ct);
        return ToShareInfoResponse(existing, budgetStatus);
    }

    public async Task<BudgetShareInfoResponse?> GetShareInfoAsync(
        int year, Guid departmentId, CancellationToken ct = default)
    {
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        var token = await db.BudgetShareTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.DepartmentBudgetId == deptBudget.Id, ct);

        if (token is null)
            return null;

        var budgetStatus = await GetBudgetStatusAsync(deptBudget.AnnualBudgetId, ct);
        return ToShareInfoResponse(token, budgetStatus);
    }

    public async Task<BudgetShareInfoResponse> UpdateShareAsync(
        int year, Guid departmentId, UpdateShareRequest request, CancellationToken ct = default)
    {
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentBudgetId == deptBudget.Id, ct)
            ?? throw new KeyNotFoundException("此部門尚未建立分享連結");

        if (request.Permission is not null)
        {
            if (!Enum.TryParse<SharePermission>(request.Permission, ignoreCase: true, out var perm))
                throw new ArgumentException("無效的權限值");
            token.Permission = perm;
        }

        if (request.IsActive.HasValue)
            token.IsActive = request.IsActive.Value;

        token.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var budgetStatus = await GetBudgetStatusAsync(deptBudget.AnnualBudgetId, ct);
        return ToShareInfoResponse(token, budgetStatus);
    }

    public async Task RevokeShareAsync(int year, Guid departmentId, CancellationToken ct = default)
    {
        var deptBudget = await FindDepartmentBudgetAsync(year, departmentId, ct);
        var token = await db.BudgetShareTokens
            .FirstOrDefaultAsync(t => t.DepartmentBudgetId == deptBudget.Id, ct)
            ?? throw new KeyNotFoundException("此部門尚未建立分享連結");

        db.BudgetShareTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("撤銷分享 Token: Year={Year}, DeptId={DeptId}", year, departmentId);
    }

    // ── 公開端 ───────────────────────────────────

    public async Task<PublicBudgetResponse> GetBudgetByTokenAsync(
        string token, CancellationToken ct = default)
    {
        var share = await ResolveTokenAsync(token, ct);
        var deptBudget = share.DepartmentBudget;
        var budgetStatus = deptBudget.AnnualBudget.Status;

        var items = await db.BudgetItems
            .AsNoTracking()
            .Where(i => i.DepartmentBudgetId == deptBudget.Id)
            .OrderBy(i => i.Sequence)
            .Select(i => new PublicBudgetItemResponse(
                i.Id, i.Sequence, i.ActivityName, i.ContentItem, i.Amount, i.Note))
            .ToListAsync(ct);

        return new PublicBudgetResponse(
            deptBudget.Department.Name,
            deptBudget.AnnualBudget.Year,
            ComputeEffectivePermission(share.Permission, budgetStatus),
            budgetStatus.ToString(),
            items);
    }

    public async Task<List<PublicBudgetItemResponse>> SaveItemsByTokenAsync(
        string token, PublicSaveBudgetItemsRequest request, CancellationToken ct = default)
    {
        var share = await ResolveTokenAsync(token, ct);
        var budgetStatus = share.DepartmentBudget.AnnualBudget.Status;
        var effective = ComputeEffectivePermission(share.Permission, budgetStatus);

        if (effective != nameof(SharePermission.Editable))
            throw new UnauthorizedAccessException("目前為唯讀模式，無法儲存");

        var deptBudgetId = share.DepartmentBudgetId;
        var existingItems = await db.BudgetItems
            .Where(i => i.DepartmentBudgetId == deptBudgetId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        RemoveDeletedItems(existingItems, request.Items);
        UpsertItems(existingItems, request.Items, deptBudgetId, now);

        share.DepartmentBudget.UpdatedAt = now;
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
            .Include(t => t.DepartmentBudget)
                .ThenInclude(d => d.AnnualBudget)
            .Include(t => t.DepartmentBudget)
                .ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(t => t.Token == token, ct)
            ?? throw new KeyNotFoundException("分享連結不存在或已失效");

        if (!share.IsActive)
            throw new UnauthorizedAccessException("此分享連結已停用");

        return share;
    }

    private async Task<DepartmentBudget> FindDepartmentBudgetAsync(
        int year, Guid departmentId, CancellationToken ct)
        => await db.DepartmentBudgets
            .Include(d => d.AnnualBudget)
            .FirstOrDefaultAsync(d => d.AnnualBudget.Year == year && d.DepartmentId == departmentId, ct)
            ?? throw new KeyNotFoundException($"{year} 年度中找不到部門 {departmentId} 的預算");

    private async Task<BudgetStatus> GetBudgetStatusAsync(Guid annualBudgetId, CancellationToken ct)
    {
        var budget = await db.AnnualBudgets
            .AsNoTracking()
            .FirstAsync(b => b.Id == annualBudgetId, ct);
        return budget.Status;
    }

    private static string ComputeEffectivePermission(SharePermission tokenPermission, BudgetStatus budgetStatus)
        => budgetStatus > BudgetStatus.Draft
            ? nameof(SharePermission.ReadOnly)
            : tokenPermission.ToString();

    private static BudgetShareInfoResponse ToShareInfoResponse(BudgetShareToken token, BudgetStatus budgetStatus)
        => new(
            token.Id,
            token.Token,
            token.Permission.ToString(),
            token.IsActive,
            ComputeEffectivePermission(token.Permission, budgetStatus),
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
