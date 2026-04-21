using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Finance;

public class BankInitialBalanceConfiguration : IEntityTypeConfiguration<BankInitialBalance>
{
    public void Configure(EntityTypeBuilder<BankInitialBalance> e)
    {
        e.HasKey(b => b.Id);
        e.Property(b => b.BankName).HasMaxLength(100).IsRequired();
        e.Property(b => b.InitialAmount).HasPrecision(18, 2);
        e.HasIndex(b => b.BankName).IsUnique();
    }
}
