using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Todos;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.Services;

/// <summary>待辦事項專案服務</summary>
public class TodoProjectService(ApplicationDbContext db) : ITodoProjectService
{
    /// <inheritdoc />
    public async Task<List<ProjectResponse>> GetProjectsAsync(string userId, CancellationToken ct = default)
    {
        return await db.TodoProjects
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Color,
                p.Order,
                p.IsArchived,
                p.Items.Count(i => i.Status != TodoStatus.Done)))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> CreateProjectAsync(
        string userId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var maxOrder = await db.TodoProjects
            .Where(p => p.UserId == userId)
            .Select(p => (int?)p.Order)
            .MaxAsync(ct) ?? -1;

        var project = new TodoProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Color = request.Color,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.TodoProjects.Add(project);
        await db.SaveChangesAsync(ct);

        return new ProjectResponse(project.Id, project.Name, project.Color, project.Order, project.IsArchived, 0);
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> UpdateProjectAsync(
        string userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await db.TodoProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的專案");

        project.Name = request.Name;
        project.Color = request.Color;
        project.IsArchived = request.IsArchived;
        project.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var itemCount = await db.TodoItems
            .CountAsync(i => i.ProjectId == projectId && i.Status != TodoStatus.Done, ct);

        return new ProjectResponse(project.Id, project.Name, project.Color, project.Order, project.IsArchived, itemCount);
    }

    /// <inheritdoc />
    public async Task DeleteProjectAsync(string userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await db.TodoProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("找不到指定的專案");

        db.TodoProjects.Remove(project);
        await db.SaveChangesAsync(ct);
    }
}
