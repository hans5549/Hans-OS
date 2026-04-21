using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class TodoCategoryService(ApplicationDbContext db) : ITodoCategoryService
{
    public async Task<List<CategoryResponse>> GetAllAsync(string userId, CancellationToken ct = default)
        => await db.TodoCategories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.Color, c.Icon, c.SortOrder))
            .ToListAsync(ct);

    public async Task<CategoryResponse> CreateAsync(string userId, CreateCategoryRequest request, CancellationToken ct = default)
    {
        var exists = await db.TodoCategories
            .AnyAsync(c => c.UserId == userId && c.Name == request.Name, ct);
        if (exists)
            throw new ArgumentException($"分類名稱「{request.Name}」已存在");

        var utcNow = DateTime.UtcNow;
        var category = new TodoCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

        db.TodoCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryResponse(category.Id, category.Name, category.Color, category.Icon, category.SortOrder);
    }

    public async Task<CategoryResponse> UpdateAsync(string userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的分類");

        var nameConflict = await db.TodoCategories
            .AnyAsync(c => c.UserId == userId && c.Name == request.Name && c.Id != id, ct);
        if (nameConflict)
            throw new ArgumentException($"分類名稱「{request.Name}」已存在");

        category.Name = request.Name;
        category.Color = request.Color;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return new CategoryResponse(category.Id, category.Name, category.Color, category.Icon, category.SortOrder);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var category = await db.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的分類");

        db.TodoCategories.Remove(category);
        await db.SaveChangesAsync(ct);
    }
}
