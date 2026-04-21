using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtendTodoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: This migration was rewritten to work from the AddTodoEntities schema state.
            // AddTodoEntities (20260421072520) ran first in production and already:
            //   - Dropped the old TodoModule tables (TodoChecklistItems, TodoItemRelations,
            //     TodoItemTodoTag, TodoItems, TodoTags, TodoCategories)
            //   - Created new TodoProjects and simplified TodoItems (no ParentId/CategoryId FK)
            // This migration adds the delta to reach the final desired schema.

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

            // Add columns missing from AddTodoEntities' simplified TodoItems schema
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "TodoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "TodoItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TodoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "TodoItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RecurrencePattern",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledDate",
                table: "TodoItems",
                type: "date",
                nullable: true);

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

            // PostgreSQL cannot auto-cast varchar → integer; use CASE expression via USING.
            migrationBuilder.Sql("""
                ALTER TABLE "FinanceTasks"
                ALTER COLUMN "Status" TYPE integer
                USING CASE "Status"
                    WHEN 'Pending'    THEN 0
                    WHEN 'InProgress' THEN 1
                    WHEN 'Completed'  THEN 2
                    ELSE 0
                END;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "FinanceTasks"
                ALTER COLUMN "Priority" TYPE integer
                USING CASE "Priority"
                    WHEN 'High'   THEN 0
                    WHEN 'Medium' THEN 1
                    WHEN 'Low'    THEN 2
                    ELSE 1
                END;
                """);

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

            migrationBuilder.AlterColumn<int>(
                name: "SourceType",
                table: "ArticleBookmarks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

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

            // Recreate tables dropped by AddTodoEntities (needed for final schema)
            migrationBuilder.CreateTable(
                name: "TodoCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoCategories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoTags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemTag",
                columns: table => new
                {
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemTag", x => new { x.TodoItemId, x.TodoTagId });
                    table.ForeignKey(
                        name: "FK_TodoItemTag_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemTag_TodoTags_TodoTagId",
                        column: x => x.TodoTagId,
                        principalTable: "TodoTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoChecklistItems_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_DeletedAt",
                table: "TodoItems",
                columns: new[] { "UserId", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoCategories_UserId_Name",
                table: "TodoCategories",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_UserId_Name",
                table: "TodoTags",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoChecklistItems_TodoItemId",
                table: "TodoChecklistItems",
                column: "TodoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId",
                table: "ArticleBookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarkGroups_UserId",
                table: "ArticleBookmarkGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemTag_TodoTagId",
                table: "TodoItemTag",
                column: "TodoTagId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId",
                principalTable: "TodoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoCategories_CategoryId",
                table: "TodoItems",
                column: "CategoryId",
                principalTable: "TodoCategories",
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

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoItems_ParentId",
                table: "TodoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_TodoCategories_CategoryId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoItemTag");

            migrationBuilder.DropTable(
                name: "TodoChecklistItems");

            migrationBuilder.DropTable(
                name: "TodoTags");

            migrationBuilder.DropTable(
                name: "TodoCategories");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_DeletedAt",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarkGroups_UserId",
                table: "ArticleBookmarkGroups");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "TodoItems");

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
                name: "Status",
                table: "FinanceTasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "FinanceTasks",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

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
                name: "SourceType",
                table: "ArticleBookmarks",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

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
                filter: "\"SourceType\" = 'InternalArticle' AND \"SourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_SourceType",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleBookmarks_UserId_Url",
                table: "ArticleBookmarks",
                columns: new[] { "UserId", "Url" },
                unique: true,
                filter: "\"SourceType\" = 'ExternalUrl' AND \"Url\" IS NOT NULL");

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
    }
}
