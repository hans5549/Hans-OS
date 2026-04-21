using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Menu;

public class MenuConfiguration : IEntityTypeConfiguration<Entities.Menu>
{
    public void Configure(EntityTypeBuilder<Entities.Menu> e)
    {
        e.HasKey(m => m.Id);
        e.Property(m => m.Name).HasMaxLength(100).IsRequired();
        e.Property(m => m.Path).HasMaxLength(300).IsRequired();
        e.Property(m => m.Component).HasMaxLength(300);
        e.Property(m => m.Redirect).HasMaxLength(300);
        e.Property(m => m.Title).HasMaxLength(200).IsRequired();
        e.Property(m => m.Icon).HasMaxLength(200);
        e.Property(m => m.ActiveIcon).HasMaxLength(200);
        e.Property(m => m.Authority).HasMaxLength(500);
        e.Property(m => m.Badge).HasMaxLength(50);
        e.Property(m => m.BadgeType).HasMaxLength(50);
        e.Property(m => m.BadgeVariants).HasMaxLength(50);
        e.Property(m => m.Link).HasMaxLength(500);
        e.Property(m => m.IframeSrc).HasMaxLength(500);

        e.HasOne(m => m.Parent)
         .WithMany(m => m.Children)
         .HasForeignKey(m => m.ParentId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(m => m.ParentId);
    }
}
