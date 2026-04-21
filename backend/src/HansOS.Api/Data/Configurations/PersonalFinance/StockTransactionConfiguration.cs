using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.PersonalFinance;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.UserId).IsRequired();
        e.Property(t => t.StockSymbol).HasMaxLength(20).IsRequired();
        e.Property(t => t.StockName).HasMaxLength(100).IsRequired();
        e.Property(t => t.TradeType)
         .HasConversion<string>()
         .HasMaxLength(10);
        e.Property(t => t.PricePerShare).HasPrecision(18, 4);
        e.Property(t => t.Commission).HasPrecision(18, 2);
        e.Property(t => t.Tax).HasPrecision(18, 2);
        e.Property(t => t.Note).HasMaxLength(500);

        e.HasOne(t => t.User)
         .WithMany()
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(t => new { t.UserId, t.StockSymbol });
        e.HasIndex(t => new { t.UserId, t.TradeDate });
    }
}
