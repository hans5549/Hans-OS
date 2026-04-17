using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Content;

public class ArticleBookmarkGroupConfiguration : IEntityTypeConfiguration<ArticleBookmarkGroup>
{
    public void Configure(EntityTypeBuilder<ArticleBookmarkGroup> e)
    {
        e.HasKey(g => g.Id);
        e.Property(g => g.UserId).IsRequired();
        e.Property(g => g.Name).HasMaxLength(100).IsRequired();

        e.HasOne(g => g.User)
         .WithMany()
         .HasForeignKey(g => g.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(g => new { g.UserId, g.Name }).IsUnique();
        e.HasIndex(g => new { g.UserId, g.SortOrder });
    }
}
