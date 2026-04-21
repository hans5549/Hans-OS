using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Activities;

public class ActivityExpenseConfiguration : IEntityTypeConfiguration<ActivityExpense>
{
    public void Configure(EntityTypeBuilder<ActivityExpense> e)
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Description).HasMaxLength(200).IsRequired();
        e.Property(x => x.Amount).HasPrecision(18, 2);
        e.Property(x => x.Note).HasMaxLength(1000);

        e.HasOne(x => x.Activity)
         .WithMany(a => a.Expenses)
         .HasForeignKey(x => x.ActivityId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Group)
         .WithMany(g => g.Expenses)
         .HasForeignKey(x => x.ActivityGroupId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.BudgetItem)
         .WithMany(b => b.LinkedExpenses)
         .HasForeignKey(x => x.BudgetItemId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(x => x.ActivityId);
        e.HasIndex(x => x.ActivityGroupId);
        e.HasIndex(x => x.BudgetItemId);
    }
}
