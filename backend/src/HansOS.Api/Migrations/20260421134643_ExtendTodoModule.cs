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
                name: "FK_TodoItems_TodoItems_RecurrenceSourceId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoItemRelations");

            migrationBuilder.DropTable(
                name: "TodoItemTodoTag");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_ArchivedAt",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_CategoryId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_ParentId_SortOrder",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_ReminderAt",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_Status_DueDate",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoChecklistItems_TodoItemId_SortOrder",
                table: "TodoChecklistItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoCategories_UserId_SortOrder",
                table: "TodoCategories");

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

            migrationBuilder.DropColumn(
                name: "ReminderAt",
                table: "TodoItems");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "TodoItems",
                newName: "Order");

            migrationBuilder.RenameColumn(
                name: "RecurrenceSourceId",
                table: "TodoItems",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TodoItems_RecurrenceSourceId",
                table: "TodoItems",
                newName: "IX_TodoItems_ProjectId");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "TodoChecklistItems",
                newName: "Order");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "RecurrencePattern",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Difficulty",
                table: "TodoItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TodoItems",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoChecklistItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TodoChecklistItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TodoChecklistItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "TodoCategories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TodoCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FinanceTasks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "FinanceTasks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

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
                name: "TodoProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#3B82F6"),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoProjects_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_DueDate",
                table: "TodoItems",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_ProjectId",
                table: "TodoItems",
                columns: new[] { "UserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_Status",
                table: "TodoItems",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoChecklistItems_TodoItemId",
                table: "TodoChecklistItems",
                column: "TodoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoCategories_UserId_Name",
                table: "TodoCategories",
                columns: new[] { "UserId", "Name" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TodoProjects_UserId_Order",
                table: "TodoProjects",
                columns: new[] { "UserId", "Order" });

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
                name: "FK_TodoItems_TodoProjects_ProjectId",
                table: "TodoItems",
                column: "ProjectId",
                principalTable: "TodoProjects",
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
                name: "FK_TodoItems_TodoProjects_ProjectId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoItemTag");

            migrationBuilder.DropTable(
                name: "TodoProjects");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_DueDate",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_ProjectId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId_Status",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoChecklistItems_TodoItemId",
                table: "TodoChecklistItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoCategories_UserId_Name",
                table: "TodoCategories");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarks_UserId",
                table: "ArticleBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ArticleBookmarkGroups_UserId",
                table: "ArticleBookmarkGroups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TodoChecklistItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TodoChecklistItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TodoCategories");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "TodoItems",
                newName: "RecurrenceSourceId");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "TodoItems",
                newName: "SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_TodoItems_ProjectId",
                table: "TodoItems",
                newName: "IX_TodoItems_RecurrenceSourceId");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "TodoChecklistItems",
                newName: "SortOrder");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TodoItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrencePattern",
                table: "TodoItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "TodoItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Difficulty",
                table: "TodoItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "TodoItems",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderAt",
                table: "TodoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoChecklistItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "TodoCategories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

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

            migrationBuilder.CreateTable(
                name: "TodoItemRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemRelations", x => x.Id);
                    table.CheckConstraint("CK_TodoItemRelation_NoSelfReference", "\"SourceItemId\" != \"TargetItemId\"");
                    table.ForeignKey(
                        name: "FK_TodoItemRelations_TodoItems_SourceItemId",
                        column: x => x.SourceItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemRelations_TodoItems_TargetItemId",
                        column: x => x.TargetItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemTodoTag",
                columns: table => new
                {
                    TagsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoItemsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemTodoTag", x => new { x.TagsId, x.TodoItemsId });
                    table.ForeignKey(
                        name: "FK_TodoItemTodoTag_TodoItems_TodoItemsId",
                        column: x => x.TodoItemsId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemTodoTag_TodoTags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "TodoTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_ArchivedAt",
                table: "TodoItems",
                columns: new[] { "UserId", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_CategoryId",
                table: "TodoItems",
                columns: new[] { "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_ParentId_SortOrder",
                table: "TodoItems",
                columns: new[] { "UserId", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_ReminderAt",
                table: "TodoItems",
                columns: new[] { "UserId", "ReminderAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_Status_DueDate",
                table: "TodoItems",
                columns: new[] { "UserId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoChecklistItems_TodoItemId_SortOrder",
                table: "TodoChecklistItems",
                columns: new[] { "TodoItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoCategories_UserId_SortOrder",
                table: "TodoCategories",
                columns: new[] { "UserId", "SortOrder" });

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

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemRelations_SourceItemId_TargetItemId_RelationType",
                table: "TodoItemRelations",
                columns: new[] { "SourceItemId", "TargetItemId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemRelations_TargetItemId",
                table: "TodoItemRelations",
                column: "TargetItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemTodoTag_TodoItemsId",
                table: "TodoItemTodoTag",
                column: "TodoItemsId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId",
                principalTable: "TodoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_TodoItems_RecurrenceSourceId",
                table: "TodoItems",
                column: "RecurrenceSourceId",
                principalTable: "TodoItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
