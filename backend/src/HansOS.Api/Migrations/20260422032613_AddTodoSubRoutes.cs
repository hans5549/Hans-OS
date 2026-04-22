using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoSubRoutes : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string TodoListId = "e0000001-0000-0000-0000-000000000001";

        private const string TodoInboxId = "e0000001-0000-0000-0001-000000000001";
        private const string TodoTodayId = "e0000001-0000-0000-0001-000000000002";
        private const string TodoUpcomingId = "e0000001-0000-0000-0001-000000000003";
        private const string TodoWeekId = "e0000001-0000-0000-0001-000000000004";
        private const string TodoAllId = "e0000001-0000-0000-0001-000000000005";
        private const string TodoTrashId = "e0000001-0000-0000-0001-000000000006";
        private const string TodoProjectId = "e0000001-0000-0000-0001-000000000007";
        private const string TodoTagId = "e0000001-0000-0000-0001-000000000008";

        private static readonly (string Id, string Name, string Path, int Order)[] SubRoutes =
        [
            (TodoInboxId,    "TodoInbox",    "/todo/inbox",        1),
            (TodoTodayId,    "TodoToday",    "/todo/today",        2),
            (TodoUpcomingId, "TodoUpcoming", "/todo/upcoming",     3),
            (TodoWeekId,     "TodoWeek",     "/todo/week",         4),
            (TodoAllId,      "TodoAll",      "/todo/all",          5),
            (TodoTrashId,    "TodoTrash",    "/todo/trash",        6),
            (TodoProjectId,  "TodoProject",  "/todo/project/:id",  7),
            (TodoTagId,      "TodoTag",      "/todo/tag/:id",      8),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, name, path, order) in SubRoutes)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "Menus" (
                        "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                        "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                        "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                        "KeepAlive", "NoBasicLayout", "ActiveIcon"
                    ) VALUES (
                        '{id}', '{TodoListId}', '{name}', '{path}',
                        '/dashboard/todo/index', NULL,
                        '代辦清單', NULL, {order},
                        false, 2, true,
                        true, false, false, false,
                        false, false, NULL
                    ) ON CONFLICT ("Id") DO NOTHING;
                    """);

                migrationBuilder.Sql($"""
                    INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                    VALUES ('{AdminRoleId}', '{id}')
                    ON CONFLICT DO NOTHING;
                    """);
            }

            // 更新父路由 redirect 至 /todo/today
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Redirect" = '/todo/today'
                WHERE "Id" = '{TodoListId}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var ids = string.Join(", ", SubRoutes.Select(r => $"'{r.Id}'"));

            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({ids});
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({ids});
                """);

            // 還原父路由 redirect
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Redirect" = '/todo/index'
                WHERE "Id" = '{TodoListId}';
                """);
        }
    }
}

