using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixUserHomePath : Migration
    {
        private const string HansUserId = "b2c3d4e5-0002-0002-0002-000000000002";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: HansUserId,
                column: "HomePath",
                value: "/dashboard/analytics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: HansUserId,
                column: "HomePath",
                value: "/analytics");
        }
    }
}
