using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todo;

public class TodoTagConfiguration : IEntityTypeConfiguration<TodoTag>
{
    public void Configure(EntityTypeBuilder<TodoTag> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.UserId).IsRequired();
        e.Property(t => t.Name).HasMaxLength(50).IsRequired();
        e.Property(t => t.Color).HasMaxLength(20);

        e.HasOne(t => t.User)
         .WithMany()
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}
