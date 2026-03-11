using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CGMSportFinance.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedEssentialData : Migration
    {
        private const string SuperRoleId = "a1b2c3d4-0001-0001-0001-000000000001";
        private const string AdminRoleId = "a1b2c3d4-0001-0001-0001-000000000002";
        private const string UserRoleId = "a1b2c3d4-0001-0001-0001-000000000003";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: ["Id", "Name", "NormalizedName", "ConcurrencyStamp"],
                values: new object[,]
                {
                    { SuperRoleId, "super", "SUPER", "00000000-0000-0000-0000-000000000001" },
                    { AdminRoleId, "admin", "ADMIN", "00000000-0000-0000-0000-000000000002" },
                    { UserRoleId, "user", "USER", "00000000-0000-0000-0000-000000000003" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValues: [SuperRoleId, AdminRoleId, UserRoleId]);
        }
    }
}
