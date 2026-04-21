using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todo;

public class TodoChecklistItemConfiguration : IEntityTypeConfiguration<TodoChecklistItem>
{
    public void Configure(EntityTypeBuilder<TodoChecklistItem> e)
    {
        e.HasKey(c => c.Id);
        e.Property(c => c.Title).HasMaxLength(200).IsRequired();

        e.HasOne(c => c.TodoItem)
         .WithMany(t => t.ChecklistItems)
         .HasForeignKey(c => c.TodoItemId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(c => new { c.TodoItemId, c.SortOrder });
    }
}
