namespace HansOS.Api.Data.Entities;

/// <summary>活動費待匯款狀態</summary>
public enum PendingRemittanceStatus
{
    Pending = 0,
    Completed = 1,
}

/// <summary>活動費待匯款紀錄</summary>
public class PendingRemittance
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SourceAccount { get; set; } = string.Empty;
    public string TargetAccount { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? RecipientName { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? Note { get; set; }
    public PendingRemittanceStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SportsDepartment? Department { get; set; }
}
