using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Activities;

public class ActivityGroupConfiguration : IEntityTypeConfiguration<ActivityGroup>
{
    public void Configure(EntityTypeBuilder<ActivityGroup> e)
    {
        e.HasKey(g => g.Id);
        e.Property(g => g.Name).HasMaxLength(200).IsRequired();

        e.HasOne(g => g.Activity)
         .WithMany(a => a.Groups)
         .HasForeignKey(g => g.ActivityId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
