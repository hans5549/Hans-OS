using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceTasks_SportsDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "SportsDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_CreatedAt",
                table: "FinanceTasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_DepartmentId",
                table: "FinanceTasks",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_DueDate",
                table: "FinanceTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_Status",
                table: "FinanceTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceTasks");
        }
    }
}
