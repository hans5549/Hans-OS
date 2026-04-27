using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class MergeChecklistIntoTodoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "__TodoChecklistMigrationMap" (
                    "TodoItemId" uuid PRIMARY KEY
                );

                INSERT INTO "__TodoChecklistMigrationMap" ("TodoItemId")
                SELECT c."Id"
                FROM "TodoChecklistItems" c
                ON CONFLICT ("TodoItemId") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "TodoItems" (
                    "Id",
                    "UserId",
                    "ProjectId",
                    "ParentId",
                    "CategoryId",
                    "Title",
                    "Description",
                    "Priority",
                    "Status",
                    "Difficulty",
                    "DueDate",
                    "ScheduledDate",
                    "Order",
                    "RecurrencePattern",
                    "RecurrenceInterval",
                    "CreatedAt",
                    "UpdatedAt",
                    "CompletedAt",
                    "ArchivedAt",
                    "DeletedAt"
                )
                SELECT
                    c."Id",
                    parent."UserId",
                    parent."ProjectId",
                    c."TodoItemId",
                    parent."CategoryId",
                    c."Title",
                    NULL,
                    0,
                    CASE WHEN c."IsCompleted" THEN 2 ELSE 0 END,
                    0,
                    NULL,
                    NULL,
                    c."Order",
                    0,
                    1,
                    c."CreatedAt",
                    c."UpdatedAt",
                    CASE WHEN c."IsCompleted" THEN c."UpdatedAt" ELSE NULL END,
                    NULL,
                    NULL
                FROM "TodoChecklistItems" c
                INNER JOIN "TodoItems" parent ON parent."Id" = c."TodoItemId"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "TodoItems" existing WHERE existing."Id" = c."Id"
                );
                """);

            migrationBuilder.DropTable(
                name: "TodoChecklistItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_ParentId",
                table: "TodoItems");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ParentId_Order",
                table: "TodoItems",
                columns: new[] { "ParentId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "TodoItems" child
                        INNER JOIN "__TodoChecklistMigrationMap" m ON m."TodoItemId" = child."Id"
                        LEFT JOIN "TodoItems" parent ON parent."Id" = child."ParentId"
                        WHERE child."Description" IS NOT NULL
                           OR child."ParentId" IS NULL
                           OR parent."Id" IS NULL
                           OR child."Priority" <> 0
                           OR child."Difficulty" <> 0
                           OR child."DueDate" IS NOT NULL
                           OR child."ScheduledDate" IS NOT NULL
                           OR child."Status" NOT IN (0, 2)
                           OR child."CompletedAt" IS DISTINCT FROM CASE WHEN child."Status" = 2 THEN child."UpdatedAt" ELSE NULL END
                           OR child."RecurrencePattern" <> 0
                           OR child."RecurrenceInterval" <> 1
                           OR child."ArchivedAt" IS NOT NULL
                           OR child."DeletedAt" IS NOT NULL
                           OR child."UserId" IS DISTINCT FROM parent."UserId"
                           OR child."ProjectId" IS DISTINCT FROM parent."ProjectId"
                           OR child."CategoryId" IS DISTINCT FROM parent."CategoryId"
                           OR EXISTS (
                               SELECT 1 FROM "TodoItemTag" tag WHERE tag."TodoItemId" = child."Id"
                           )
                    ) THEN
                        RAISE EXCEPTION 'MergeChecklistIntoTodoItem rollback blocked: one or more converted child TodoItems contain data that cannot be represented as checklist items.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_ParentId_Order",
                table: "TodoItems");

            migrationBuilder.CreateTable(
                name: "TodoChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TodoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                name: "IX_TodoChecklistItems_TodoItemId",
                table: "TodoChecklistItems",
                column: "TodoItemId");

            migrationBuilder.Sql(
                """
                INSERT INTO "TodoChecklistItems" (
                    "Id",
                    "TodoItemId",
                    "Title",
                    "IsCompleted",
                    "Order",
                    "CreatedAt",
                    "UpdatedAt"
                )
                SELECT
                    child."Id",
                    child."ParentId",
                    child."Title",
                    child."Status" = 2,
                    child."Order",
                    child."CreatedAt",
                    child."UpdatedAt"
                FROM "TodoItems" child
                INNER JOIN "__TodoChecklistMigrationMap" m ON m."TodoItemId" = child."Id"
                WHERE child."ParentId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "TodoChecklistItems" existing WHERE existing."Id" = child."Id"
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "TodoItems" child
                USING "__TodoChecklistMigrationMap" m
                WHERE child."Id" = m."TodoItemId";

                DROP TABLE "__TodoChecklistMigrationMap";
                """);
        }
    }
}
