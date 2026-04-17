using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class BudgetItemConfiguration : IEntityTypeConfiguration<BudgetItem>
{
    public void Configure(EntityTypeBuilder<BudgetItem> e)
    {
        e.HasKey(bi => bi.Id);
        e.HasOne(bi => bi.DepartmentBudget)
         .WithMany(db => db.Items)
         .HasForeignKey(bi => bi.DepartmentBudgetId)
         .OnDelete(DeleteBehavior.Cascade);
        e.Property(bi => bi.ActivityName).HasMaxLength(200).IsRequired();
        e.Property(bi => bi.ContentItem).HasMaxLength(200).IsRequired();
        e.Property(bi => bi.Amount).HasPrecision(18, 2);
        e.Property(bi => bi.ActualAmount).HasPrecision(18, 2);
        e.Property(bi => bi.Note).HasMaxLength(1000);
        e.Property(bi => bi.ActualNote).HasMaxLength(1000);
    }
}
