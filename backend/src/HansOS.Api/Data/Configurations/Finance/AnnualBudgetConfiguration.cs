using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class AnnualBudgetConfiguration : IEntityTypeConfiguration<AnnualBudget>
{
    public void Configure(EntityTypeBuilder<AnnualBudget> e)
    {
        e.HasKey(b => b.Id);
        e.Property(b => b.Year).IsRequired();
        e.HasIndex(b => b.Year).IsUnique();
        e.Property(b => b.Status)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(b => b.Note).HasMaxLength(1000);
        e.Property(b => b.GrantedBudget).HasPrecision(18, 2);
    }
}
