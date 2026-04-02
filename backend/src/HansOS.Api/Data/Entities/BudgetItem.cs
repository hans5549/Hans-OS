namespace HansOS.Api.Data.Entities;

/// <summary>預算項目</summary>
public class BudgetItem
{
    public Guid Id { get; set; }
    public Guid DepartmentBudgetId { get; set; }
    public int Sequence { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string ContentItem { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public decimal? ActualAmount { get; set; }
    public string? ActualNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DepartmentBudget DepartmentBudget { get; set; } = null!;

    /// <summary>連結到此預算項目的活動開銷</summary>
    public ICollection<ActivityExpense> LinkedExpenses { get; set; } = [];
}
