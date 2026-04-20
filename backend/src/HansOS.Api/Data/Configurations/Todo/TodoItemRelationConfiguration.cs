using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Todo;

public class TodoItemRelationConfiguration : IEntityTypeConfiguration<TodoItemRelation>
{
    public void Configure(EntityTypeBuilder<TodoItemRelation> e)
    {
        e.HasKey(r => r.Id);
        e.Property(r => r.RelationType)
         .HasConversion<string>()
         .HasMaxLength(20);

        e.HasOne(r => r.SourceItem)
         .WithMany(t => t.SourceRelations)
         .HasForeignKey(r => r.SourceItemId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(r => r.TargetItem)
         .WithMany(t => t.TargetRelations)
         .HasForeignKey(r => r.TargetItemId)
         .OnDelete(DeleteBehavior.Restrict);

        // 禁止自引用
        e.ToTable(t => t.HasCheckConstraint(
            "CK_TodoItemRelation_NoSelfReference",
            "\"SourceItemId\" != \"TargetItemId\""));

        // 同一 Source/Target/Type 組合唯一
        e.HasIndex(r => new { r.SourceItemId, r.TargetItemId, r.RelationType })
         .IsUnique();
    }
}
