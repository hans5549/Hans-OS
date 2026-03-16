using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedEssentialData : Migration
    {
        // Fixed GUIDs for seed data
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string DashboardMenuId = "d1e2f3a4-0000-0000-0000-000000000001";
        private const string AnalyticsMenuId = "d1e2f3a4-0000-0000-0000-000000000002";
        private const string WorkspaceMenuId = "d1e2f3a4-0000-0000-0000-000000000003";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Insert admin role (Identity uses string Id)
            migrationBuilder.Sql($"""
                INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
                VALUES ('{AdminRoleId}', 'admin', 'ADMIN', '{Guid.NewGuid()}')
                ON CONFLICT ("Id") DO NOTHING;
                """);

            // 2. Dashboard menu tree
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

            // 3. RoleMenu associations
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus" WHERE "RoleId" = '{AdminRoleId}';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus" WHERE "Id" IN ('{AnalyticsMenuId}', '{WorkspaceMenuId}', '{DashboardMenuId}');
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "AspNetRoles" WHERE "Id" = '{AdminRoleId}';
                """);
        }
    }
}
