using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> e)
    {
        e.Property(u => u.RealName).HasMaxLength(100);
        e.Property(u => u.Avatar).HasMaxLength(500);
        e.Property(u => u.Desc).HasMaxLength(500);
        e.Property(u => u.HomePath).HasMaxLength(200);
    }
}
