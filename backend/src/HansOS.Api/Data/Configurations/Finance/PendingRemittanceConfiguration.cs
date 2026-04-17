using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class PendingRemittanceConfiguration : IEntityTypeConfiguration<PendingRemittance>
{
    public void Configure(EntityTypeBuilder<PendingRemittance> e)
    {
        e.HasKey(r => r.Id);
        e.Property(r => r.Description).HasMaxLength(200).IsRequired();
        e.Property(r => r.Amount).HasPrecision(18, 2);
        e.Property(r => r.SourceAccount).HasMaxLength(100).IsRequired();
        e.Property(r => r.TargetAccount).HasMaxLength(100).IsRequired();
        e.Property(r => r.RecipientName).HasMaxLength(100);
        e.Property(r => r.Note).HasMaxLength(1000);
        e.Property(r => r.Status)
         .HasConversion<string>()
         .HasMaxLength(20);

        e.HasOne(r => r.Department)
         .WithMany()
         .HasForeignKey(r => r.DepartmentId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(r => r.ActivityExpense)
         .WithMany()
         .HasForeignKey(r => r.ActivityExpenseId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(r => r.Status);
        e.HasIndex(r => r.DepartmentId);
        e.HasIndex(r => r.ActivityExpenseId);
    }
}
