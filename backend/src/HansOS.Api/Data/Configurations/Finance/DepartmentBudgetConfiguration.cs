using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class DepartmentBudgetConfiguration : IEntityTypeConfiguration<DepartmentBudget>
{
    public void Configure(EntityTypeBuilder<DepartmentBudget> e)
    {
        e.HasKey(db => db.Id);
        e.HasOne(db => db.AnnualBudget)
         .WithMany(ab => ab.DepartmentBudgets)
         .HasForeignKey(db => db.AnnualBudgetId)
         .OnDelete(DeleteBehavior.Cascade);
        e.HasOne(db => db.Department)
         .WithMany()
         .HasForeignKey(db => db.DepartmentId)
         .OnDelete(DeleteBehavior.Restrict);
        e.Property(db => db.AllocatedAmount).HasPrecision(18, 2);
        e.HasIndex(db => new { db.AnnualBudgetId, db.DepartmentId }).IsUnique();
    }
}
