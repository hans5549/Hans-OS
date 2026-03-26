namespace HansOS.Api.Data.Entities;

public class SportsDepartment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
