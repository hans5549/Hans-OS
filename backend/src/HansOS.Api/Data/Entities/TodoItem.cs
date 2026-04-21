namespace HansOS.Api.Data.Entities;

/// <summary>待辦清單任務</summary>
public class TodoItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // 狀態 & 分類
    public TodoStatus Status { get; set; }
    public TodoPriority Priority { get; set; }
    public TodoDifficulty Difficulty { get; set; }

    // 日期（Things 3 模式：ScheduledDate = 預計開始日，DueDate = 截止日）
    public DateOnly? ScheduledDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime? ReminderAt { get; set; }

    // 排序（同一 ParentId 內）
    public int SortOrder { get; set; }

    // 週期性
    public RecurrencePattern? RecurrencePattern { get; set; }
    public int RecurrenceInterval { get; set; } = 1;
    public Guid? RecurrenceSourceId { get; set; }
    public TodoItem? RecurrenceSource { get; set; }

    // 階層（子任務：自引用）
    public Guid? ParentId { get; set; }
    public TodoItem? Parent { get; set; }
    public List<TodoItem> Children { get; set; } = [];

    // Checklist（輕量核取清單）
    public List<TodoChecklistItem> ChecklistItems { get; set; } = [];

    // 分類 & 標籤
    public Guid? CategoryId { get; set; }
    public TodoCategory? Category { get; set; }
    public List<TodoTag> Tags { get; set; } = [];

    // 關聯（此任務作為 Source / Target 的所有關聯）
    public List<TodoItemRelation> SourceRelations { get; set; } = [];
    public List<TodoItemRelation> TargetRelations { get; set; } = [];

    // 生命週期
    public DateTime? CompletedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
