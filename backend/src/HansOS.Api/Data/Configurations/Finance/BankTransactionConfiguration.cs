using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.BankName).HasMaxLength(100).IsRequired();
        e.Property(t => t.Description).HasMaxLength(200).IsRequired();
        e.Property(t => t.Amount).HasPrecision(18, 0);
        e.Property(t => t.Fee).HasPrecision(18, 0);
        e.Property(t => t.TransactionType)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(t => t.ReceiptCollected).HasDefaultValue(false);

        e.HasOne(t => t.Department)
         .WithMany()
         .HasForeignKey(t => t.DepartmentId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(t => t.Activity)
         .WithMany()
         .HasForeignKey(t => t.ActivityId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(t => t.PendingRemittance)
         .WithMany()
         .HasForeignKey(t => t.PendingRemittanceId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(t => new { t.BankName, t.TransactionDate });
    }
}
