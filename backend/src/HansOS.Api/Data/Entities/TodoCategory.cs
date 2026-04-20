namespace HansOS.Api.Data.Entities;

/// <summary>待辦任務工作分類（使用者自訂，單一主分類）</summary>
public class TodoCategory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public List<TodoItem> TodoItems { get; set; } = [];
}
