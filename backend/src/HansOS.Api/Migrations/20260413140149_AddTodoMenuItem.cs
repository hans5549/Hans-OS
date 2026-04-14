using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoMenuItem : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string DashboardMenuId = "d1e2f3a4-0000-0000-0000-000000000001";
        private const string TodoMenuId = "d1e2f3a4-0000-0000-0000-000000000010";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon", "Authority"
                ) VALUES (
                    '{TodoMenuId}', '{DashboardMenuId}', 'Todo', '/todo',
                    '/dashboard/todo/index', NULL,
                    'page.dashboard.todo', NULL, 3,
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL, '["admin"]'
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{TodoMenuId}')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Authority" = '["admin"]'
                WHERE "Id" = '{TodoMenuId}'
                  AND ("Authority" IS NULL OR "Authority" = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" = '{TodoMenuId}';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" = '{TodoMenuId}';
                """);
        }
    }
}
