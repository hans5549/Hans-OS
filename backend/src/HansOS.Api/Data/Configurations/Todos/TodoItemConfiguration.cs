using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todos;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.Title).HasMaxLength(500).IsRequired();
        e.Property(t => t.Description).HasMaxLength(2000);
        e.Property(t => t.Priority).HasConversion<int>();
        e.Property(t => t.Status).HasConversion<int>();
        e.Property(t => t.Difficulty).HasConversion<int>();
        e.Property(t => t.RecurrencePattern).HasConversion<int>();

        e.HasOne(t => t.User)
         .WithMany()
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(t => t.Project)
         .WithMany(p => p.Items)
         .HasForeignKey(t => t.ProjectId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasOne(t => t.Parent)
         .WithMany(t => t.Children)
         .HasForeignKey(t => t.ParentId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(t => t.Category)
         .WithMany()
         .HasForeignKey(t => t.CategoryId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(t => new { t.UserId, t.Status });
        e.HasIndex(t => new { t.UserId, t.DueDate });
        e.HasIndex(t => new { t.UserId, t.ProjectId });
        e.HasIndex(t => new { t.UserId, t.DeletedAt });
    }
}
