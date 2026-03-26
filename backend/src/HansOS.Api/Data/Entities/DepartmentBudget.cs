namespace HansOS.Api.Data.Entities;

/// <summary>部門預算</summary>
public class DepartmentBudget
{
    public Guid Id { get; set; }
    public Guid AnnualBudgetId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AnnualBudget AnnualBudget { get; set; } = null!;
    public SportsDepartment Department { get; set; } = null!;
    public ICollection<BudgetItem> Items { get; set; } = [];
}
