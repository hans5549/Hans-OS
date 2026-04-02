namespace HansOS.Api.Data.Entities;

/// <summary>預算分享 Token 權限</summary>
public enum SharePermission
{
    /// <summary>唯讀</summary>
    ReadOnly = 0,

    /// <summary>可編輯</summary>
    Editable = 1,
}

/// <summary>預算分享連結 Token</summary>
public class BudgetShareToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid DepartmentBudgetId { get; set; }
    public SharePermission Permission { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DepartmentBudget DepartmentBudget { get; set; } = null!;
}
