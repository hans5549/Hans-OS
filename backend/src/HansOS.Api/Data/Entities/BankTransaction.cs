namespace HansOS.Api.Data.Entities;

public enum TransactionType
{
    Income = 0,
    Expense = 1,
}

public class BankTransaction
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public bool ReceiptCollected { get; set; }
    public bool ReceiptMailed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? ActivityId { get; set; }
    public Guid? PendingRemittanceId { get; set; }

    public SportsDepartment? Department { get; set; }
    public Activity? Activity { get; set; }
    public PendingRemittance? PendingRemittance { get; set; }
}
