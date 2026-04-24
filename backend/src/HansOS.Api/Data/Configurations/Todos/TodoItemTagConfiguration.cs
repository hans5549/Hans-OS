using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todos;

public class TodoItemTagConfiguration : IEntityTypeConfiguration<TodoItemTag>
{
    public void Configure(EntityTypeBuilder<TodoItemTag> e)
    {
        e.HasKey(t => new { t.TodoItemId, t.TodoTagId });

        e.HasOne(t => t.TodoItem)
         .WithMany(i => i.TodoItemTags)
         .HasForeignKey(t => t.TodoItemId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(t => t.TodoTag)
         .WithMany(tag => tag.TodoItemTags)
         .HasForeignKey(t => t.TodoTagId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
