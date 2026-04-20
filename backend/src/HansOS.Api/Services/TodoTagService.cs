using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todo;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>待辦標籤 CRUD 服務</summary>
public class TodoTagService(
    ApplicationDbContext db,
    ILogger<TodoTagService> logger) : ITodoTagService
{
    public async Task<List<TodoTagResponse>> GetTagsAsync(
        string userId, CancellationToken ct = default)
    {
        return await db.TodoTags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new TodoTagResponse(t.Id, t.Name, t.Color, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TodoTagResponse> CreateTagAsync(
        string userId, CreateTodoTagRequest request, CancellationToken ct = default)
    {
        var nameExists = await db.TodoTags
            .AnyAsync(t => t.UserId == userId && t.Name == request.Name, ct);

        if (nameExists)
        {
            throw new ArgumentException($"標籤名稱「{request.Name}」已存在");
        }

        var tag = new TodoTag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow,
        };

        db.TodoTags.Add(tag);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦標籤已建立: {TagId}, 名稱={Name}", tag.Id, tag.Name);

        return new TodoTagResponse(tag.Id, tag.Name, tag.Color, tag.CreatedAt);
    }

    public async Task<TodoTagResponse> UpdateTagAsync(
        string userId, Guid tagId, UpdateTodoTagRequest request, CancellationToken ct = default)
    {
        var tag = await db.TodoTags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("標籤不存在");

        if (request.Name != tag.Name)
        {
            var nameExists = await db.TodoTags
                .AnyAsync(t => t.UserId == userId
                    && t.Name == request.Name
                    && t.Id != tagId, ct);

            if (nameExists)
            {
                throw new ArgumentException($"標籤名稱「{request.Name}」已存在");
            }
        }

        tag.Name = request.Name;
        tag.Color = request.Color;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦標籤已更新: {TagId}", tagId);

        return new TodoTagResponse(tag.Id, tag.Name, tag.Color, tag.CreatedAt);
    }

    public async Task DeleteTagAsync(
        string userId, Guid tagId, CancellationToken ct = default)
    {
        var tag = await db.TodoTags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("標籤不存在");

        db.TodoTags.Remove(tag);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("待辦標籤已刪除: {TagId}", tagId);
    }
}
