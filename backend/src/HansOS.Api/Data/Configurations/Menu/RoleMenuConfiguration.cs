using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HansOS.Api.Data.Configurations.Menu;

public class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> e)
    {
        e.HasKey(rm => new { rm.RoleId, rm.MenuId });

        e.HasOne(rm => rm.Menu)
         .WithMany(m => m.RoleMenus)
         .HasForeignKey(rm => rm.MenuId)
         .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(rm => rm.RoleId);
    }
}
