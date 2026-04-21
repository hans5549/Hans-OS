using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

public class TodoTagService(ApplicationDbContext db) : ITodoTagService
{
    public async Task<List<TagResponse>> GetAllAsync(string userId, CancellationToken ct = default)
        => await db.TodoTags
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name, t.Color))
            .ToListAsync(ct);

    public async Task<TagResponse> CreateAsync(string userId, CreateTagRequest request, CancellationToken ct = default)
    {
        var exists = await db.TodoTags
            .AnyAsync(t => t.UserId == userId && t.Name == request.Name, ct);
        if (exists)
            throw new ArgumentException($"標籤名稱「{request.Name}」已存在");

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
        return new TagResponse(tag.Id, tag.Name, tag.Color);
    }

    public async Task<TagResponse> UpdateAsync(string userId, Guid id, UpdateTagRequest request, CancellationToken ct = default)
    {
        var tag = await db.TodoTags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的標籤");

        var nameConflict = await db.TodoTags
            .AnyAsync(t => t.UserId == userId && t.Name == request.Name && t.Id != id, ct);
        if (nameConflict)
            throw new ArgumentException($"標籤名稱「{request.Name}」已存在");

        tag.Name = request.Name;
        tag.Color = request.Color;

        await db.SaveChangesAsync(ct);
        return new TagResponse(tag.Id, tag.Name, tag.Color);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var tag = await db.TodoTags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的標籤");

        db.TodoTags.Remove(tag);
        await db.SaveChangesAsync(ct);
    }
}
