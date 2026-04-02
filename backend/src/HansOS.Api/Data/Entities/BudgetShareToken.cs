namespace HansOS.Api.Data.Entities;

/// <summary>預算分享 Token 權限</summary>
public enum SharePermission
{
    /// <summary>唯讀</summary>
    ReadOnly = 0,

    /// <summary>可編輯</summary>
    Editable = 1,
}

/// <summary>部門預算分享連結 Token（每個部門一個永久連結）</summary>
public class BudgetShareToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public SharePermission Permission { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SportsDepartment Department { get; set; } = null!;
}
