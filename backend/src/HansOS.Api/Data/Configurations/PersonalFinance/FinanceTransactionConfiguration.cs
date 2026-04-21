using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.PersonalFinance;

public class FinanceTransactionConfiguration : IEntityTypeConfiguration<FinanceTransaction>
{
    public void Configure(EntityTypeBuilder<FinanceTransaction> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.UserId).IsRequired();
        e.Property(t => t.TransactionType)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(t => t.Amount).HasPrecision(18, 2);
        e.Property(t => t.Currency).HasMaxLength(3).HasDefaultValue("TWD");
        e.Property(t => t.Project).HasMaxLength(100);
        e.Property(t => t.Tags).HasMaxLength(500);
        e.Property(t => t.Note).HasMaxLength(500);

        e.HasOne(t => t.User)
         .WithMany()
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(t => t.Category)
         .WithMany()
         .HasForeignKey(t => t.CategoryId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(t => t.Account)
         .WithMany()
         .HasForeignKey(t => t.AccountId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(t => t.ToAccount)
         .WithMany()
         .HasForeignKey(t => t.ToAccountId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(t => new { t.UserId, t.TransactionDate });
        e.HasIndex(t => new { t.UserId, t.CategoryId });
        e.HasIndex(t => new { t.UserId, t.AccountId });
    }
}
