using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Activities;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> e)
    {
        e.HasKey(a => a.Id);
        e.Property(a => a.Name).HasMaxLength(200).IsRequired();
        e.Property(a => a.Description).HasMaxLength(1000);
        e.Property(a => a.Month).IsRequired();
        e.Property(a => a.Year).IsRequired();

        e.HasOne(a => a.Department)
         .WithMany()
         .HasForeignKey(a => a.DepartmentId)
         .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(a => new { a.DepartmentId, a.Year, a.Month });
    }
}
