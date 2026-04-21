namespace HansOS.Api.Data.Entities;

/// <summary>待辦事項優先級</summary>
public enum TodoPriority
{
    /// <summary>無優先級（P4 灰）</summary>
    None = 0,

    /// <summary>低優先級（P3 藍）</summary>
    Low = 1,

    /// <summary>中優先級（P2 橙）</summary>
    Medium = 2,

    /// <summary>高優先級（P1 紅）</summary>
    High = 3,
}

/// <summary>待辦事項狀態</summary>
public enum TodoStatus
{
    /// <summary>待處理</summary>
    Pending = 0,

    /// <summary>進行中</summary>
    InProgress = 1,

    /// <summary>已完成</summary>
    Done = 2,
}

/// <summary>待辦事項</summary>
public class TodoItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>所屬專案；null 表示 Inbox</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>標題，最長 500 字元</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>描述，最長 2000 字元</summary>
    public string? Description { get; set; }

    public TodoPriority Priority { get; set; }
    public TodoStatus Status { get; set; }

    /// <summary>截止日期</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>排列順序</summary>
    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>完成時間；Done 狀態時設定</summary>
    public DateTime? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public TodoProject? Project { get; set; }
}
