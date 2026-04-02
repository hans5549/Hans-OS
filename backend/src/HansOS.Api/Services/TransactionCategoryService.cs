using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>交易分類服務（支援系統預設 + 使用者自訂分類）</summary>
public class TransactionCategoryService(
    ApplicationDbContext db,
    ILogger<TransactionCategoryService> logger) : ITransactionCategoryService
{
    /// <inheritdoc />
    public async Task<List<CategoryResponse>> GetCategoriesAsync(
        string userId, string? type = null, CancellationToken ct = default)
    {
        var query = db.TransactionCategories
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.UserId == null);

        if (type is not null)
        {
            if (!Enum.TryParse<CategoryType>(type, ignoreCase: true, out var categoryType))
            {
                throw new ArgumentException("無效的分類類型");
            }

            query = query.Where(c => c.CategoryType == categoryType);
        }

        var allCategories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

        return BuildCategoryTree(allCategories);
    }

    /// <inheritdoc />
    public async Task<CategoryResponse> CreateCategoryAsync(
        string userId, CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CategoryType>(request.CategoryType, ignoreCase: true, out var categoryType))
        {
            throw new ArgumentException("無效的分類類型");
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await db.TransactionCategories
                .AnyAsync(c => c.Id == request.ParentId.Value
                    && (c.UserId == userId || c.UserId == null), ct);

            if (!parentExists)
            {
                throw new KeyNotFoundException("父分類不存在");
            }
        }

        var nameExists = await db.TransactionCategories
            .AnyAsync(c => c.UserId == userId
                && c.ParentId == request.ParentId
                && c.Name == request.Name, ct);

        if (nameExists)
        {
            throw new ArgumentException($"分類名稱「{request.Name}」在此層級已存在");
        }

        var category = new TransactionCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ParentId = request.ParentId,
            Name = request.Name,
            Icon = request.Icon,
            CategoryType = categoryType,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        db.TransactionCategories.Add(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("分類已建立: {CategoryId}, 名稱={Name}", category.Id, category.Name);

        return new CategoryResponse(
            category.Id,
            category.Name,
            category.CategoryType.ToString(),
            category.Icon,
            category.SortOrder,
            false,
            null);
    }

    /// <inheritdoc />
    public async Task<CategoryResponse> UpdateCategoryAsync(
        string userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.TransactionCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new KeyNotFoundException("分類不存在");

        if (category.UserId is null)
        {
            throw new ArgumentException("系統預設分類無法編輯");
        }

        if (category.UserId != userId)
        {
            throw new KeyNotFoundException("分類不存在");
        }

        var nameExists = await db.TransactionCategories
            .AnyAsync(c => c.UserId == userId
                && c.ParentId == category.ParentId
                && c.Name == request.Name
                && c.Id != categoryId, ct);

        if (nameExists)
        {
            throw new ArgumentException($"分類名稱「{request.Name}」在此層級已存在");
        }

        category.Name = request.Name;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("分類已更新: {CategoryId}", categoryId);

        return new CategoryResponse(
            category.Id,
            category.Name,
            category.CategoryType.ToString(),
            category.Icon,
            category.SortOrder,
            false,
            null);
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(
        string userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await db.TransactionCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("分類不存在");

        if (category.UserId is null)
        {
            throw new ArgumentException("系統預設分類無法刪除");
        }

        var hasTransactions = await db.FinanceTransactions
            .AnyAsync(t => t.CategoryId == categoryId, ct);

        if (hasTransactions)
        {
            throw new ArgumentException("此分類已有交易記錄，無法刪除");
        }

        var hasChildren = await db.TransactionCategories
            .AnyAsync(c => c.ParentId == categoryId, ct);

        if (hasChildren)
        {
            throw new ArgumentException("此分類包含子分類，請先刪除子分類");
        }

        db.TransactionCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("分類已刪除: {CategoryId}", categoryId);
    }

    /// <summary>將扁平分類清單建構為樹狀結構</summary>
    private static List<CategoryResponse> BuildCategoryTree(List<TransactionCategory> categories)
    {
        var lookup = categories.ToLookup(c => c.ParentId);

        List<CategoryResponse> BuildChildren(Guid? parentId)
        {
            return lookup[parentId]
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.Name,
                    c.CategoryType.ToString(),
                    c.Icon,
                    c.SortOrder,
                    c.UserId is null,
                    BuildChildren(c.Id) is { Count: > 0 } children ? children : null))
                .ToList();
        }

        return BuildChildren(null);
    }
}
