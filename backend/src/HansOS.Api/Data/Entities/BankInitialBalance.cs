namespace HansOS.Api.Data.Entities;

public class BankInitialBalance
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public decimal InitialAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
