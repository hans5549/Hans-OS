namespace HansOS.Api.Data.Entities;

/// <summary>個人記帳交易類型</summary>
public enum FinanceTransactionType
{
    /// <summary>支出</summary>
    Expense = 1,

    /// <summary>收入</summary>
    Income = 2,

    /// <summary>轉帳（帳戶間轉移）</summary>
    Transfer = 3,
}

/// <summary>個人記帳交易記錄</summary>
public class FinanceTransaction
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public FinanceTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Note { get; set; }

    /// <summary>分類 ID；轉帳時為 null</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>來源帳戶（支出/轉帳的扣款帳戶，收入的入帳帳戶）</summary>
    public Guid AccountId { get; set; }

    /// <summary>目標帳戶；僅轉帳時使用</summary>
    public Guid? ToAccountId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public TransactionCategory? Category { get; set; }
    public FinanceAccount Account { get; set; } = null!;
    public FinanceAccount? ToAccount { get; set; }
}
