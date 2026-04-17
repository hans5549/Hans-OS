using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Content;

public class ArticleBookmarkConfiguration : IEntityTypeConfiguration<ArticleBookmark>
{
    public void Configure(EntityTypeBuilder<ArticleBookmark> e)
    {
        e.HasKey(b => b.Id);
        e.Property(b => b.UserId).IsRequired();
        e.Property(b => b.SourceType)
         .HasConversion<string>()
         .HasMaxLength(30);
        e.Property(b => b.SourceId).HasMaxLength(200);
        e.Property(b => b.Url).HasMaxLength(2048);
        e.Property(b => b.Title).HasMaxLength(300).IsRequired();
        e.Property(b => b.CustomTitle).HasMaxLength(300);
        e.Property(b => b.ExcerptSnapshot).HasMaxLength(1000);
        e.Property(b => b.CoverImageUrl).HasMaxLength(2048);
        e.Property(b => b.Domain).HasMaxLength(255);
        e.Property(b => b.Note).HasMaxLength(1000);
        e.Property(b => b.Tags)
         .HasColumnType("text[]")
         .HasDefaultValueSql("ARRAY[]::text[]");

        e.HasOne(b => b.User)
         .WithMany()
         .HasForeignKey(b => b.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(b => b.Group)
         .WithMany()
         .HasForeignKey(b => b.GroupId)
         .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(b => new { b.UserId, b.Url })
         .IsUnique()
         .HasFilter("\"SourceType\" = 'ExternalUrl' AND \"Url\" IS NOT NULL");
        e.HasIndex(b => new { b.UserId, b.SourceId })
         .IsUnique()
         .HasFilter("\"SourceType\" = 'InternalArticle' AND \"SourceId\" IS NOT NULL");
        e.HasIndex(b => new { b.UserId, b.SourceType });
        e.HasIndex(b => new { b.UserId, b.GroupId });
        e.HasIndex(b => new { b.UserId, b.CreatedAt });
    }
}
