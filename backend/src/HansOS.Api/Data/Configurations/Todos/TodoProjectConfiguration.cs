using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todos;

public class TodoProjectConfiguration : IEntityTypeConfiguration<TodoProject>
{
    public void Configure(EntityTypeBuilder<TodoProject> e)
    {
        e.HasKey(p => p.Id);
        e.Property(p => p.Name).HasMaxLength(100).IsRequired();
        e.Property(p => p.Color).HasMaxLength(20).HasDefaultValue("#3B82F6");

        e.HasOne(p => p.User)
         .WithMany()
         .HasForeignKey(p => p.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(p => new { p.UserId, p.Order });
    }
}
