using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBudgetShareWithDepartmentShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetShareTokens_DepartmentBudgets_DepartmentBudgetId",
                table: "BudgetShareTokens");

            migrationBuilder.RenameColumn(
                name: "DepartmentBudgetId",
                table: "BudgetShareTokens",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetShareTokens_DepartmentBudgetId",
                table: "BudgetShareTokens",
                newName: "IX_BudgetShareTokens_DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetShareTokens_SportsDepartments_DepartmentId",
                table: "BudgetShareTokens",
                column: "DepartmentId",
                principalTable: "SportsDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetShareTokens_SportsDepartments_DepartmentId",
                table: "BudgetShareTokens");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "BudgetShareTokens",
                newName: "DepartmentBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetShareTokens_DepartmentId",
                table: "BudgetShareTokens",
                newName: "IX_BudgetShareTokens_DepartmentBudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetShareTokens_DepartmentBudgets_DepartmentBudgetId",
                table: "BudgetShareTokens",
                column: "DepartmentBudgetId",
                principalTable: "DepartmentBudgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
