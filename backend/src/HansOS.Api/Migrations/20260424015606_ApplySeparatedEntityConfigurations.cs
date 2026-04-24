using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class ApplySeparatedEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                table: "ArticleBookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceTasks_SportsDepartments_DepartmentId",
                table: "FinanceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarkGroups_UserId",
                table: "ArticleBookmarkGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "FinanceTransactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Project",
                table: "FinanceTransactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceTransactions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TWD",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinanceTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FinanceTasks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceAccounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TWD",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ArticleBookmarks",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ArticleBookmarks",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string[]>(
                name: "Tags",
                table: "ArticleBookmarks",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]",
                oldClrType: typeof(string[]),
                oldType: "text[]");

            migrationBuilder.AlterColumn<string>(
                name: "SourceId",
                table: "ArticleBookmarks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ArticleBookmarks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExcerptSnapshot",
                table: "ArticleBookmarks",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Domain",
                table: "ArticleBookmarks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomTitle",
                table: "ArticleBookmarks",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "ArticleBookmarks",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ArticleBookmarkGroups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_CreatedAt",
                table: "FinanceTasks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_DueDate",
                table: "FinanceTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTasks_Status",
                table: "FinanceTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_CreatedAt",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_GroupId",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_SourceId",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "SourceId" },
                unique: true,
                filter: "\"SourceType\" = 2 AND \"SourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_SourceType",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_Url",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "Url" },
                unique: true,
                filter: "\"SourceType\" = 1 AND \"Url\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId_Name",
                table: "ArticleBookmarkGroups",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId_SortOrder",
                table: "ArticleBookmarkGroups",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                table: "ArticleBookmarks",
                column: "GroupId",
                principalTable: "ArticleBookmarkGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceTasks_SportsDepartments_DepartmentId",
                table: "FinanceTasks",
                column: "DepartmentId",
                principalTable: "SportsDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                table: "ArticleBookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceTasks_SportsDepartments_DepartmentId",
                table: "FinanceTasks");

            migrationBuilder.DropIndex(
                name: "IX_FinanceTasks_CreatedAt",
                table: "FinanceTasks");

            migrationBuilder.DropIndex(
                name: "IX_FinanceTasks_DueDate",
                table: "FinanceTasks");

            migrationBuilder.DropIndex(
                name: "IX_FinanceTasks_Status",
                table: "FinanceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId_CreatedAt",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId_GroupId",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId_SourceId",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId_SourceType",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId_Url",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarkGroups_UserId_Name",
                table: "ArticleBookmarkGroups");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarkGroups_UserId_SortOrder",
                table: "ArticleBookmarkGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "FinanceTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Project",
                table: "FinanceTransactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceTransactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "TWD");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "FinanceTasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FinanceTasks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "FinanceAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "TWD");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ArticleBookmarks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string[]>(
                name: "Tags",
                table: "ArticleBookmarks",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldDefaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AlterColumn<string>(
                name: "SourceId",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExcerptSnapshot",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Domain",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomTitle",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "ArticleBookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ArticleBookmarkGroups",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId",
                table: "ArticleBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId",
                table: "ArticleBookmarkGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                table: "ArticleBookmarks",
                column: "GroupId",
                principalTable: "ArticleBookmarkGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceTasks_SportsDepartments_DepartmentId",
                table: "FinanceTasks",
                column: "DepartmentId",
                principalTable: "SportsDepartments",
                principalColumn: "Id");
        }
    }
}
