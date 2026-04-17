using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Data;

/// <summary>
/// 跨 Service 共用的 DbContext 驗證擴充方法。
/// 取代散落在各 Service 中的重複驗證邏輯，例外型別與訊息保持與原實作一致。
/// </summary>
public static class ApplicationDbContextValidationExtensions
{
    /// <summary>
    /// 驗證指定的部門 ID 必須存在；若不存在拋出 <see cref="ArgumentException"/>。
    /// </summary>
    public static async Task EnsureDepartmentExistsAsync(
        this ApplicationDbContext db,
        Guid departmentId,
        CancellationToken ct = default)
    {
        var exists = await db.SportsDepartments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId, ct);

        if (!exists)
            throw new ArgumentException("指定的部門不存在");
    }

    /// <summary>
    /// 驗證可選的部門 ID — 若有提供則必須存在；不提供則無事發生。
    /// </summary>
    public static async Task EnsureDepartmentExistsIfProvidedAsync(
        this ApplicationDbContext db,
        Guid? departmentId,
        CancellationToken ct = default)
    {
        if (!departmentId.HasValue)
            return;

        await db.EnsureDepartmentExistsAsync(departmentId.Value, ct);
    }
}
