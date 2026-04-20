namespace HansOS.Api.Data.Entities;

/// <summary>輕量核取清單項目（無日期、無狀態、無優先級）</summary>
public class TodoChecklistItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }

    public Guid TodoItemId { get; set; }
    public TodoItem TodoItem { get; set; } = null!;
}
