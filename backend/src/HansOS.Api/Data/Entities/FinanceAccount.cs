namespace HansOS.Api.Data.Entities;

/// <summary>帳戶類型</summary>
public enum FinanceAccountType
{
    /// <summary>現金</summary>
    Cash = 1,

    /// <summary>銀行帳戶</summary>
    Bank = 2,

    /// <summary>信用卡</summary>
    CreditCard = 3,

    /// <summary>電子支付</summary>
    EPayment = 4,

    /// <summary>投資帳戶</summary>
    Investment = 5,
}

/// <summary>個人記帳帳戶</summary>
public class FinanceAccount
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FinanceAccountType AccountType { get; set; }
    public decimal InitialBalance { get; set; }

    /// <summary>帳戶幣別（ISO 4217），預設 TWD</summary>
    public string Currency { get; set; } = "TWD";

    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
