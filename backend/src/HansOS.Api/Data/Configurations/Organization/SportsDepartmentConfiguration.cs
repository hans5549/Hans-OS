using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Organization;

public class SportsDepartmentConfiguration : IEntityTypeConfiguration<SportsDepartment>
{
    public void Configure(EntityTypeBuilder<SportsDepartment> e)
    {
        e.HasKey(d => d.Id);
        e.Property(d => d.Name).HasMaxLength(100).IsRequired();
        e.Property(d => d.Note).HasMaxLength(500);
        e.HasIndex(d => d.Name).IsUnique();
    }
}
