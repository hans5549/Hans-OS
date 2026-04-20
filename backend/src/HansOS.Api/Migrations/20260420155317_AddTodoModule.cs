using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "TodoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReminderAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TodoCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoItems_RecurrenceSourceId",
                        column: x => x.RecurrenceSourceId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TodoChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_TodoCategories_UserId_SortOrder",
                table: "TodoCategories",
                columns: new[] { "UserId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoChecklistItems_TodoItemId_SortOrder",
                table: "TodoChecklistItems",
                columns: new[] { "TodoItemId", "SortOrder" });

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
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_RecurrenceSourceId",
                table: "TodoItems",
                column: "RecurrenceSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_ArchivedAt",
                table: "TodoItems",
                columns: new[] { "UserId", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_CategoryId",
                table: "TodoItems",
                columns: new[] { "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_DeletedAt",
                table: "TodoItems",
                columns: new[] { "UserId", "DeletedAt" });

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
                name: "IX_TodoItemTodoTag_TodoItemsId",
                table: "TodoItemTodoTag",
                column: "TodoItemsId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_UserId_Name",
                table: "TodoTags",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoChecklistItems");

            migrationBuilder.DropTable(
                name: "TodoItemRelations");

            migrationBuilder.DropTable(
                name: "TodoItemTodoTag");

            migrationBuilder.DropTable(
                name: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoTags");

            migrationBuilder.DropTable(
                name: "TodoCategories");
        }
    }
}
