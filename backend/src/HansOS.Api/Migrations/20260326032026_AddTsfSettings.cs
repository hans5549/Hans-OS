using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTsfSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankInitialBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InitialAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankInitialBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SportsDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportsDepartments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankInitialBalances_BankName",
                table: "BankInitialBalances",
                column: "BankName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportsDepartments_Name",
                table: "SportsDepartments",
                column: "Name",
                unique: true);

            // Seed 兩筆銀行起始資料
            var now = DateTime.UtcNow.ToString("o");
            migrationBuilder.Sql($"""
                INSERT INTO "BankInitialBalances" ("Id", "BankName", "InitialAmount", "CreatedAt", "UpdatedAt")
                VALUES
                    ('b0000001-0000-0000-0000-000000000001', '上海銀行', 0, '{now}', '{now}'),
                    ('b0000001-0000-0000-0000-000000000002', '合作金庫', 0, '{now}', '{now}')
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankInitialBalances");

            migrationBuilder.DropTable(
                name: "SportsDepartments");
        }
    }
}
