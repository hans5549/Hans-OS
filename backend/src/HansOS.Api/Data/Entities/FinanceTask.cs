namespace HansOS.Api.Data.Entities;

/// <summary>財務任務優先度</summary>
public enum FinanceTaskPriority
{
    High = 0,
    Medium = 1,
    Low = 2,
}

/// <summary>財務任務狀態</summary>
public enum FinanceTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
}

/// <summary>一般性財務待辦任務</summary>
public class FinanceTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FinanceTaskPriority Priority { get; set; }
    public FinanceTaskStatus Status { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? DepartmentId { get; set; }
    public SportsDepartment? Department { get; set; }
}
