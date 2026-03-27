namespace HansOS.Api.Data.Entities;

/// <summary>年度預算狀態</summary>
public enum BudgetStatus
{
    /// <summary>草稿</summary>
    Draft = 0,

    /// <summary>已提交</summary>
    Submitted = 1,

    /// <summary>已核准</summary>
    Approved = 2,

    /// <summary>已結算</summary>
    Settled = 3,
}

/// <summary>年度預算</summary>
public class AnnualBudget
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public BudgetStatus Status { get; set; }
    public string? Note { get; set; }

    /// <summary>核定總預算（今年實際拿到的總預算金額）</summary>
    public decimal? GrantedBudget { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<DepartmentBudget> DepartmentBudgets { get; set; } = [];
}
