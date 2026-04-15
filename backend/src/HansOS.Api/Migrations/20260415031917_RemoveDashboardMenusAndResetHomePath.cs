using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDashboardMenusAndResetHomePath : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string DashboardMenuId = "d1e2f3a4-0000-0000-0000-000000000001";
        private const string AnalyticsMenuId = "d1e2f3a4-0000-0000-0000-000000000002";
        private const string WorkspaceMenuId = "d1e2f3a4-0000-0000-0000-000000000003";
        private const string TodoMenuId = "d1e2f3a4-0000-0000-0000-000000000010";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ('{TodoMenuId}', '{AnalyticsMenuId}', '{WorkspaceMenuId}', '{DashboardMenuId}');
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ('{TodoMenuId}', '{AnalyticsMenuId}', '{WorkspaceMenuId}');
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" = '{DashboardMenuId}';
                """);

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers"
                SET "HomePath" = '/index'
                WHERE "HomePath" IN ('/dashboard', '/analytics', '/workspace', '/todo');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{DashboardMenuId}', NULL, 'Dashboard', '/dashboard',
                    'BasicLayout', '/analytics',
                    'page.dashboard.title', 'lucide:layout-dashboard', -1,
                    false, 1, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{AnalyticsMenuId}', '{DashboardMenuId}', 'Analytics', '/analytics',
                    '/dashboard/analytics/index', NULL,
                    'page.dashboard.analytics', NULL, 1,
                    true, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{WorkspaceMenuId}', '{DashboardMenuId}', 'Workspace', '/workspace',
                    '/dashboard/workspace/index', NULL,
                    'page.dashboard.workspace', NULL, 2,
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

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
                VALUES ('{AdminRoleId}', '{DashboardMenuId}')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{AnalyticsMenuId}')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{WorkspaceMenuId}')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{TodoMenuId}')
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers"
                SET "HomePath" = '/analytics'
                WHERE "HomePath" = '/index';
                """);

        }
    }
}
