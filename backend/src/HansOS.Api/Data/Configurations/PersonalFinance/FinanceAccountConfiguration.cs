using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.PersonalFinance;

public class FinanceAccountConfiguration : IEntityTypeConfiguration<FinanceAccount>
{
    public void Configure(EntityTypeBuilder<FinanceAccount> e)
    {
        e.HasKey(a => a.Id);
        e.Property(a => a.UserId).IsRequired();
        e.Property(a => a.Name).HasMaxLength(100).IsRequired();
        e.Property(a => a.AccountType)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(a => a.InitialBalance).HasPrecision(18, 2);
        e.Property(a => a.Currency).HasMaxLength(3).HasDefaultValue("TWD");
        e.Property(a => a.Icon).HasMaxLength(200);

        e.HasOne(a => a.User)
         .WithMany()
         .HasForeignKey(a => a.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(a => new { a.UserId, a.Name }).IsUnique();
    }
}
