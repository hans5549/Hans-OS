using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceReportsMenu : Migration
    {
        private const string FinanceBookkeepingId = "b0000001-0000-0000-0000-000000000001";
        private const string FinanceReportsId = "b0000001-0000-0000-0000-000000000006";
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{FinanceReportsId}', '{FinanceBookkeepingId}',
                    'FinanceReports', '/finance/reports',
                    '/finance/reports/index', NULL,
                    '報表分析', 'lucide:bar-chart-3', 4,
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{FinanceReportsId}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" = '{FinanceReportsId}';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" = '{FinanceReportsId}';
                """);
        }
    }
}
