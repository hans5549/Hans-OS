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

    /// <summary>緊急優先級（P0 紫）</summary>
    Urgent = 4,
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

/// <summary>待辦事項難度</summary>
public enum TodoDifficulty
{
    None = 0,
    Easy = 1,
    Medium = 2,
    Hard = 3,
}

/// <summary>重複週期類型</summary>
public enum RecurrencePattern
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4,
}

/// <summary>待辦事項</summary>
public class TodoItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>所屬專案；null 表示 Inbox</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>父任務 ID；null 表示頂層任務</summary>
    public Guid? ParentId { get; set; }

    /// <summary>所屬分類</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>標題，最長 500 字元</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>描述，最長 2000 字元</summary>
    public string? Description { get; set; }

    public TodoPriority Priority { get; set; }
    public TodoStatus Status { get; set; }
    public TodoDifficulty Difficulty { get; set; }

    /// <summary>截止日期</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>計畫日期</summary>
    public DateOnly? ScheduledDate { get; set; }

    /// <summary>排列順序</summary>
    public int Order { get; set; }

    /// <summary>重複週期</summary>
    public RecurrencePattern RecurrencePattern { get; set; }

    /// <summary>重複間隔數（搭配 RecurrencePattern）</summary>
    public int RecurrenceInterval { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>完成時間；Done 狀態時設定</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>封存時間；已封存時設定</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>軟刪除時間；已刪除時設定</summary>
    public DateTime? DeletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public TodoProject? Project { get; set; }
    public TodoItem? Parent { get; set; }
    public TodoCategory? Category { get; set; }
    public ICollection<TodoItem> Children { get; set; } = [];
    public ICollection<TodoChecklistItem> ChecklistItems { get; set; } = [];
    public ICollection<TodoItemTag> TodoItemTags { get; set; } = [];
}

/// <summary>待辦事項分類</summary>
public class TodoCategory
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

/// <summary>待辦事項標籤</summary>
public class TodoTag
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<TodoItemTag> TodoItemTags { get; set; } = [];
}

/// <summary>待辦事項與標籤的中間表</summary>
public class TodoItemTag
{
    public Guid TodoItemId { get; set; }
    public Guid TodoTagId { get; set; }

    public TodoItem TodoItem { get; set; } = null!;
    public TodoTag TodoTag { get; set; } = null!;
}

/// <summary>待辦事項檢查清單項目</summary>
public class TodoChecklistItem
{
    public Guid Id { get; set; }
    public Guid TodoItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TodoItem TodoItem { get; set; } = null!;
}
