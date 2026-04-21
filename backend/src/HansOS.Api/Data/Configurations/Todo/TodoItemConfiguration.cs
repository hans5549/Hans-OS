using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todo;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> e)
    {
        e.HasKey(t => t.Id);
        e.Property(t => t.UserId).IsRequired();
        e.Property(t => t.Title).HasMaxLength(200).IsRequired();
        e.Property(t => t.Description).HasMaxLength(10000);

        // Enum → string 存儲
        e.Property(t => t.Status)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(t => t.Priority)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(t => t.Difficulty)
         .HasConversion<string>()
         .HasMaxLength(20);
        e.Property(t => t.RecurrencePattern)
         .HasConversion<string>()
         .HasMaxLength(20);

        // 軟刪除 global query filter
        e.HasQueryFilter(t => t.DeletedAt == null);

        // User
        e.HasOne(t => t.User)
         .WithMany()
         .HasForeignKey(t => t.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // 自引用階層（子任務）
        e.HasOne(t => t.Parent)
         .WithMany(t => t.Children)
         .HasForeignKey(t => t.ParentId)
         .OnDelete(DeleteBehavior.SetNull);

        // 週期性系列來源
        e.HasOne(t => t.RecurrenceSource)
         .WithMany()
         .HasForeignKey(t => t.RecurrenceSourceId)
         .OnDelete(DeleteBehavior.SetNull);

        // 分類
        e.HasOne(t => t.Category)
         .WithMany(c => c.TodoItems)
         .HasForeignKey(t => t.CategoryId)
         .OnDelete(DeleteBehavior.SetNull);

        // 標籤 M2M skip navigation
        e.HasMany(t => t.Tags)
         .WithMany(t => t.TodoItems)
         .UsingEntity("TodoItemTodoTag");

        // Indexes
        e.HasIndex(t => new { t.UserId, t.Status, t.DueDate });
        e.HasIndex(t => new { t.UserId, t.ParentId, t.SortOrder });
        e.HasIndex(t => new { t.UserId, t.DeletedAt });
        e.HasIndex(t => new { t.UserId, t.ArchivedAt });
        e.HasIndex(t => new { t.UserId, t.ReminderAt });
        e.HasIndex(t => new { t.UserId, t.CategoryId });
    }
}
