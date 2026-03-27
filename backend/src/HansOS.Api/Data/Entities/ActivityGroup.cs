namespace HansOS.Api.Data.Entities;

/// <summary>活動分組</summary>
public class ActivityGroup
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Activity Activity { get; set; } = null!;
    public ICollection<ActivityExpense> Expenses { get; set; } = [];
}
