namespace HansOS.Api.Data.Entities;

/// <summary>待辦任務標籤（跨分類軟標記，多選）</summary>
public class TodoTag
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public List<TodoItem> TodoItems { get; set; } = [];
}
