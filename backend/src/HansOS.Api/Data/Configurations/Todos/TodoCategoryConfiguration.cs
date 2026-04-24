using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todos;

public class TodoCategoryConfiguration : IEntityTypeConfiguration<TodoCategory>
{
    public void Configure(EntityTypeBuilder<TodoCategory> e)
    {
        e.HasKey(c => c.Id);
        e.Property(c => c.Name).HasMaxLength(100).IsRequired();
        e.Property(c => c.Color).HasMaxLength(20);
        e.Property(c => c.Icon).HasMaxLength(100);

        e.HasOne(c => c.User)
         .WithMany()
         .HasForeignKey(c => c.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
    }
}
