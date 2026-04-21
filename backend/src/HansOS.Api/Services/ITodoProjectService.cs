using HansOS.Api.Models.Todos;

namespace HansOS.Api.Services;

public interface ITodoProjectService
{
    /// <summary>取得使用者所有專案</summary>
    Task<List<ProjectResponse>> GetProjectsAsync(string userId, CancellationToken ct = default);

    /// <summary>建立專案</summary>
    Task<ProjectResponse> CreateProjectAsync(string userId, CreateProjectRequest request, CancellationToken ct = default);

    /// <summary>更新專案</summary>
    Task<ProjectResponse> UpdateProjectAsync(string userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default);

    /// <summary>刪除專案（含所有任務）</summary>
    Task DeleteProjectAsync(string userId, Guid projectId, CancellationToken ct = default);
}
