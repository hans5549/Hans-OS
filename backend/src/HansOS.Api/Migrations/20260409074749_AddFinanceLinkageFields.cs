using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceLinkageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActivityExpenseId",
                table: "PendingRemittances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivityId",
                table: "BankTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingRemittanceId",
                table: "BankTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRemittances_ActivityExpenseId",
                table: "PendingRemittances",
                column: "ActivityExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_ActivityId",
                table: "BankTransactions",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_PendingRemittanceId",
                table: "BankTransactions",
                column: "PendingRemittanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_Activities_ActivityId",
                table: "BankTransactions",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_PendingRemittances_PendingRemittanceId",
                table: "BankTransactions",
                column: "PendingRemittanceId",
                principalTable: "PendingRemittances",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingRemittances_ActivityExpenses_ActivityExpenseId",
                table: "PendingRemittances",
                column: "ActivityExpenseId",
                principalTable: "ActivityExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_Activities_ActivityId",
                table: "BankTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_PendingRemittances_PendingRemittanceId",
                table: "BankTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingRemittances_ActivityExpenses_ActivityExpenseId",
                table: "PendingRemittances");

            migrationBuilder.DropIndex(
                name: "IX_PendingRemittances_ActivityExpenseId",
                table: "PendingRemittances");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_ActivityId",
                table: "BankTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_PendingRemittanceId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "ActivityExpenseId",
                table: "PendingRemittances");

            migrationBuilder.DropColumn(
                name: "ActivityId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "PendingRemittanceId",
                table: "BankTransactions");
        }
    }
}
