using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.PersonalFinance;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    public void Configure(EntityTypeBuilder<TransactionCategory> e)
    {
        e.HasKey(c => c.Id);
        e.Property(c => c.Name).HasMaxLength(100).IsRequired();
        e.Property(c => c.Icon).HasMaxLength(200);
        e.Property(c => c.CategoryType)
         .HasConversion<string>()
         .HasMaxLength(20);

        e.HasOne(c => c.User)
         .WithMany()
         .HasForeignKey(c => c.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(c => c.Parent)
         .WithMany(c => c.Children)
         .HasForeignKey(c => c.ParentId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(c => new { c.UserId, c.ParentId, c.Name }).IsUnique();
        e.HasIndex(c => c.ParentId);
    }
}
