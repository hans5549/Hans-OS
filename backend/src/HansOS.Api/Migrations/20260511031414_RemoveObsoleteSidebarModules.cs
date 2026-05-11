using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObsoleteSidebarModules : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string LegacyTodoMenuId = "d1e2f3a4-0000-0000-0000-000000000010";
        private const string HopeMediaId = "d1e2f3a4-0000-0000-0000-000000000005";
        private const string HopeMediaIndexId = "d1e2f3a4-0000-0000-0000-000000000008";
        private const string HmAnnualBudgetId = "a0000002-0000-0000-0000-000000000001";
        private const string HmAnnualPlanId = "a0000002-0000-0000-0000-000000000002";
        private const string HmLearningDocsId = "a0000002-0000-0000-0000-000000000003";
        private const string HmSurveyId = "a0000002-0000-0000-0000-000000000004";
        private const string ArticleCollectionId = "a0000004-0000-0000-0000-000000000001";
        private const string ArticleCollectionIndexId = "a0000004-0000-0000-0000-000000000002";
        private const string TodoListId = "e0000001-0000-0000-0000-000000000001";
        private const string TodoListIndexId = "e0000001-0000-0000-0000-000000000002";

        private static readonly string[] ChildMenuIds =
        [
            LegacyTodoMenuId,
            HopeMediaIndexId,
            HmAnnualBudgetId,
            HmAnnualPlanId,
            HmLearningDocsId,
            HmSurveyId,
            ArticleCollectionIndexId,
            TodoListIndexId,
        ];

        private static readonly string[] ParentMenuIds =
        [
            HopeMediaId,
            ArticleCollectionId,
            TodoListId,
        ];

        private static readonly string[] ObsoleteMenuIds =
        [
            ..ChildMenuIds,
            ..ParentMenuIds,
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var childIdList = ToSqlIdList(ChildMenuIds);
            var parentIdList = ToSqlIdList(ParentMenuIds);
            var obsoleteIdList = ToSqlIdList(ObsoleteMenuIds);

            migrationBuilder.Sql(
                """
                UPDATE "AspNetUsers"
                SET "HomePath" = '/index'
                WHERE "HomePath" IN ('/hope-media', '/todo', '/article-collection')
                   OR "HomePath" LIKE '/hope-media/%'
                   OR "HomePath" LIKE '/todo/%'
                   OR "HomePath" LIKE '/article-collection/%';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({obsoleteIdList});
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN (
                    SELECT "Id"
                    FROM "Menus"
                    WHERE "Path" IN ('/hope-media', '/todo', '/article-collection')
                       OR "Path" LIKE '/hope-media/%'
                       OR "Path" LIKE '/todo/%'
                       OR "Path" LIKE '/article-collection/%'
                );
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({childIdList});
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Menus"
                WHERE "Path" LIKE '/hope-media/%'
                   OR "Path" LIKE '/todo/%'
                   OR "Path" LIKE '/article-collection/%';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({parentIdList});
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Menus"
                WHERE "Path" IN ('/hope-media', '/todo', '/article-collection');
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "ArticleBookmarks";
                DROP TABLE IF EXISTS "TodoItemTag";
                DROP TABLE IF EXISTS "ArticleBookmarkGroups";
                DROP TABLE IF EXISTS "TodoItems";
                DROP TABLE IF EXISTS "TodoTags";
                DROP TABLE IF EXISTS "TodoCategories";
                DROP TABLE IF EXISTS "TodoProjects";
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "__TodoChecklistMigrationMap";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleBookmarkGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleBookmarkGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarkGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                name: "TodoProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#3B82F6"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "TodoTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
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
                name: "ArticleBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExcerptSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    LastOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SourceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "ARRAY[]::text[]"),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarks_ArticleBookmarkGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ArticleBookmarkGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ArticleBookmarks_AspNetUsers_UserId",
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
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "TodoProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                name: "IX_ArticleBookmarks_GroupId",
                table: "ArticleBookmarks",
                column: "GroupId");

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
                name: "IX_TodoCategories_UserId_Name",
                table: "TodoCategories",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_CategoryId",
                table: "TodoItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ParentId_Order",
                table: "TodoItems",
                columns: new[] { "ParentId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ProjectId",
                table: "TodoItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId_DeletedAt",
                table: "TodoItems",
                columns: new[] { "UserId", "DeletedAt" });

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
                name: "IX_TodoItemTag_TodoTagId",
                table: "TodoItemTag",
                column: "TodoTagId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoProjects_UserId_Order",
                table: "TodoProjects",
                columns: new[] { "UserId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoTags_UserId_Name",
                table: "TodoTags",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "__TodoChecklistMigrationMap" (
                    "TodoItemId" uuid PRIMARY KEY
                );
                """);

            InsertCatalog(
                migrationBuilder,
                HopeMediaId,
                "HopeMedia",
                "/hope-media",
                "/hope-media/annual-budget",
                "主希望光影音部",
                "lucide:video",
                2,
                hideChildrenInMenu: false);
            InsertHiddenMenu(
                migrationBuilder,
                HopeMediaIndexId,
                HopeMediaId,
                "HopeMediaIndex",
                "/hope-media/index",
                "/hope-media/index",
                "主希望光影音部");
            InsertMenu(
                migrationBuilder,
                HmAnnualBudgetId,
                HopeMediaId,
                "HmAnnualBudget",
                "/hope-media/annual-budget",
                "/hope-media/annual-budget/index",
                "年度預算",
                "lucide:calculator",
                2);
            InsertMenu(
                migrationBuilder,
                HmAnnualPlanId,
                HopeMediaId,
                "HmAnnualPlan",
                "/hope-media/annual-plan",
                "/hope-media/annual-plan/index",
                "年度計畫",
                "lucide:clipboard-list",
                3);
            InsertMenu(
                migrationBuilder,
                HmLearningDocsId,
                HopeMediaId,
                "HmLearningDocs",
                "/hope-media/learning-docs",
                "/hope-media/learning-docs/index",
                "學習文件",
                "lucide:book-open",
                4);
            InsertMenu(
                migrationBuilder,
                HmSurveyId,
                HopeMediaId,
                "HmSurvey",
                "/hope-media/survey",
                "/hope-media/survey/index",
                "調查",
                "lucide:search",
                5);

            InsertCatalog(
                migrationBuilder,
                ArticleCollectionId,
                "ArticleCollection",
                "/article-collection",
                "/article-collection/index",
                "文章收藏",
                "lucide:bookmark",
                4,
                hideChildrenInMenu: true);
            InsertHiddenMenu(
                migrationBuilder,
                ArticleCollectionIndexId,
                ArticleCollectionId,
                "ArticleCollectionIndex",
                "/article-collection/index",
                "/article-collection/index",
                "文章收藏");

            InsertCatalog(
                migrationBuilder,
                TodoListId,
                "TodoList",
                "/todo",
                "/todo/index",
                "代辦清單",
                "lucide:list-todo",
                3,
                hideChildrenInMenu: true);
            InsertHiddenMenu(
                migrationBuilder,
                TodoListIndexId,
                TodoListId,
                "TodoListIndex",
                "/todo/index",
                "/dashboard/todo/index",
                "代辦清單");

            var roleMenuValues = string.Join(",\n    ",
                new[]
                {
                    HopeMediaId,
                    HopeMediaIndexId,
                    HmAnnualBudgetId,
                    HmAnnualPlanId,
                    HmLearningDocsId,
                    HmSurveyId,
                    ArticleCollectionId,
                    ArticleCollectionIndexId,
                    TodoListId,
                    TodoListIndexId,
                }.Select(id => $"('{AdminRoleId}', '{id}')"));

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES
                    {roleMenuValues}
                ON CONFLICT DO NOTHING;
                """);
        }

        private static string ToSqlIdList(IEnumerable<string> ids)
            => string.Join(", ", ids.Select(id => $"'{id}'"));

        private static void InsertCatalog(
            MigrationBuilder migrationBuilder,
            string id,
            string name,
            string path,
            string redirect,
            string title,
            string icon,
            int order,
            bool hideChildrenInMenu)
        {
            var hideChildren = hideChildrenInMenu ? "true" : "false";

            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{id}', NULL, '{name}', '{path}',
                    'BasicLayout', '{redirect}',
                    '{title}', '{icon}', {order},
                    false, 1, true,
                    false, false, false, {hideChildren},
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        private static void InsertMenu(
            MigrationBuilder migrationBuilder,
            string id,
            string parentId,
            string name,
            string path,
            string component,
            string title,
            string icon,
            int order)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{id}', '{parentId}', '{name}', '{path}',
                    '{component}', NULL,
                    '{title}', '{icon}', {order},
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        private static void InsertHiddenMenu(
            MigrationBuilder migrationBuilder,
            string id,
            string parentId,
            string name,
            string path,
            string component,
            string title)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{id}', '{parentId}', '{name}', '{path}',
                    '{component}', NULL,
                    '{title}', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }
    }
}
