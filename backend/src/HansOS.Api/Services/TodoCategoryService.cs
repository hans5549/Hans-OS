using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todo;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>待辦分類 CRUD 服務</summary>
public class TodoCategoryService(
    ApplicationDbContext db,
    ILogger<TodoCategoryService> logger) : ITodoCategoryService
{
    public async Task<List<TodoCategoryResponse>> GetCategoriesAsync(
        string userId, CancellationToken ct = default)
    {
        return await db.TodoCategories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new TodoCategoryResponse(
                c.Id, c.Name, c.Color, c.Icon, c.SortOrder, c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TodoCategoryResponse> CreateCategoryAsync(
        string userId, CreateTodoCategoryRequest request, CancellationToken ct = default)
    {
        var nameExists = await db.TodoCategories
            .AnyAsync(c => c.UserId == userId && c.Name == request.Name, ct);

        if (nameExists)
        {
            throw new ArgumentException($"分類名稱「{request.Name}」已存在");
        }

        var category = new TodoCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
        };

        db.TodoCategories.Add(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦分類已建立: {CategoryId}, 名稱={Name}", category.Id, category.Name);

        return new TodoCategoryResponse(
            category.Id, category.Name, category.Color, category.Icon,
            category.SortOrder, category.CreatedAt);
    }

    public async Task<TodoCategoryResponse> UpdateCategoryAsync(
        string userId, Guid categoryId, UpdateTodoCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("分類不存在");

        var nameExists = await db.TodoCategories
            .AnyAsync(c => c.UserId == userId
                && c.Name == request.Name
                && c.Id != categoryId, ct);

        if (nameExists)
        {
            throw new ArgumentException($"分類名稱「{request.Name}」已存在");
        }

        category.Name = request.Name;
        category.Color = request.Color;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦分類已更新: {CategoryId}", categoryId);

        return new TodoCategoryResponse(
            category.Id, category.Name, category.Color, category.Icon,
            category.SortOrder, category.CreatedAt);
    }

    public async Task DeleteCategoryAsync(
        string userId, Guid categoryId, CancellationToken ct = default)
    {
        var category = await db.TodoCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException("分類不存在");

        var hasTodoItems = await db.TodoItems
            .AnyAsync(t => t.CategoryId == categoryId, ct);

        if (hasTodoItems)
        {
            throw new ArgumentException("此分類已有任務使用，無法刪除");
        }

        db.TodoCategories.Remove(category);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦分類已刪除: {CategoryId}", categoryId);
    }
}
