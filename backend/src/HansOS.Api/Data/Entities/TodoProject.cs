namespace HansOS.Api.Data.Entities;

/// <summary>待辦事項專案（清單）</summary>
public class TodoProject
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>專案名稱，最長 100 字元</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>顯示顏色，hex 格式，如 "#3B82F6"</summary>
    public string Color { get; set; } = "#3B82F6";

    /// <summary>排列順序</summary>
    public int Order { get; set; }

    /// <summary>是否已封存</summary>
    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public List<TodoItem> Items { get; set; } = [];
}
