using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class FinanceTaskConfiguration : IEntityTypeConfiguration<FinanceTask>
{
    public void Configure(EntityTypeBuilder<FinanceTask> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.Title).HasMaxLength(200).IsRequired();
        e.Property(t => t.Description).HasMaxLength(1000);

        e.HasOne(t => t.Department)
         .WithMany()
         .HasForeignKey(t => t.DepartmentId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(t => t.Status);
        e.HasIndex(t => t.DueDate);
        e.HasIndex(t => t.CreatedAt);
    }
}
