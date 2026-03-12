using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHansPassword : Migration
    {
        private const string HansUserId = "b2c3d4e5-0002-0002-0002-000000000002";
        private const string OldPasswordHash = "AQAAAAIAAYagAAAAEHHPkZxg2PX+DM7eDtMrk+UsWvYsnQ0Relp/xvvbv9eEO1UNc1WvfkkKiO9N4HsKHQ==";
        private const string NewPasswordHash = "AQAAAAIAAYagAAAAEMrObqmsNPuZlM6O/HTUdJvJAWn1FkswjCIORecY6+dFGw8bwjMENMINlz4nVWYehQ==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: HansUserId,
                column: "PasswordHash",
                value: NewPasswordHash);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: HansUserId,
                column: "PasswordHash",
                value: OldPasswordHash);
        }
    }
}
