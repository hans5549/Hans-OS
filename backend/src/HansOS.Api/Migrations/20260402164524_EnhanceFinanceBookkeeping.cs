using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceFinanceBookkeeping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "FinanceTransactions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TWD");

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "FinanceTransactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "FinanceTransactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "FinanceAccounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TWD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "Project",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "FinanceTransactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "FinanceAccounts");
        }
    }
}
