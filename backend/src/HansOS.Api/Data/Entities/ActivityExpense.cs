namespace HansOS.Api.Data.Entities;

/// <summary>活動開銷項目</summary>
public class ActivityExpense
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public Guid? ActivityGroupId { get; set; }
    public Guid? BudgetItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Activity Activity { get; set; } = null!;
    public ActivityGroup? Group { get; set; }
    public BudgetItem? BudgetItem { get; set; }
}
