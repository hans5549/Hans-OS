namespace HansOS.Api.Data.Entities;

/// <summary>部門活動</summary>
public class Activity
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SportsDepartment Department { get; set; } = null!;
    public ICollection<ActivityGroup> Groups { get; set; } = [];
    public ICollection<ActivityExpense> Expenses { get; set; } = [];
}
