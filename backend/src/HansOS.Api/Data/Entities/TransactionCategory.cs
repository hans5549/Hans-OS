namespace HansOS.Api.Data.Entities;

/// <summary>分類類型（支出/收入）</summary>
public enum CategoryType
{
    /// <summary>支出</summary>
    Expense = 1,

    /// <summary>收入</summary>
    Income = 2,
}

/// <summary>交易分類（兩層結構：主分類 + 子分類）</summary>
public class TransactionCategory
{
    public Guid Id { get; set; }

    /// <summary>擁有者；null 表示系統預設分類</summary>
    public string? UserId { get; set; }

    /// <summary>父分類 ID；null 表示主分類</summary>
    public Guid? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public CategoryType CategoryType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ApplicationUser? User { get; set; }
    public TransactionCategory? Parent { get; set; }
    public ICollection<TransactionCategory> Children { get; set; } = [];
}
