using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class BudgetShareTokenConfiguration : IEntityTypeConfiguration<BudgetShareToken>
{
    public void Configure(EntityTypeBuilder<BudgetShareToken> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.Token).HasMaxLength(100).IsRequired();
        e.HasIndex(t => t.Token).IsUnique();
        e.Property(t => t.Permission)
         .HasConversion<string>()
         .HasMaxLength(20);

        e.HasOne(t => t.Department)
         .WithMany()
         .HasForeignKey(t => t.DepartmentId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(t => t.DepartmentId).IsUnique();
    }
}
