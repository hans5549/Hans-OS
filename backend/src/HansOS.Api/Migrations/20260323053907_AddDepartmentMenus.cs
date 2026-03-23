using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentMenus : Migration
    {
        // Catalog parents
        private const string TaiwanSportsFinanceId = "d1e2f3a4-0000-0000-0000-000000000004";
        private const string HopeMediaId = "d1e2f3a4-0000-0000-0000-000000000005";
        private const string ZhongyuanMissionId = "d1e2f3a4-0000-0000-0000-000000000006";

        // Menu children
        private const string TaiwanSportsFinanceIndexId = "d1e2f3a4-0000-0000-0000-000000000007";
        private const string HopeMediaIndexId = "d1e2f3a4-0000-0000-0000-000000000008";
        private const string ZhongyuanMissionIndexId = "d1e2f3a4-0000-0000-0000-000000000009";

        // Existing
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Catalog: 台灣體育部財務
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{TaiwanSportsFinanceId}', NULL, 'TaiwanSportsFinance', '/taiwan-sports-finance',
                    'BasicLayout', '/taiwan-sports-finance/index',
                    '台灣體育部財務', 'lucide:wallet', 1,
                    false, 1, true,
                    false, false, false, true,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 2. Catalog: 主希望光影音部
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{HopeMediaId}', NULL, 'HopeMedia', '/hope-media',
                    'BasicLayout', '/hope-media/index',
                    '主希望光影音部', 'lucide:video', 2,
                    false, 1, true,
                    false, false, false, true,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 3. Catalog: 中原傳道團
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{ZhongyuanMissionId}', NULL, 'ZhongyuanMission', '/zhongyuan-mission',
                    'BasicLayout', '/zhongyuan-mission/index',
                    '中原傳道團', 'lucide:globe', 3,
                    false, 1, true,
                    false, false, false, true,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 4. Menu child: 台灣體育部財務 Index (hidden)
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{TaiwanSportsFinanceIndexId}', '{TaiwanSportsFinanceId}', 'TaiwanSportsFinanceIndex', '/taiwan-sports-finance/index',
                    '/taiwan-sports-finance/index', NULL,
                    '台灣體育部財務', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 5. Menu child: 主希望光影音部 Index (hidden)
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{HopeMediaIndexId}', '{HopeMediaId}', 'HopeMediaIndex', '/hope-media/index',
                    '/hope-media/index', NULL,
                    '主希望光影音部', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 6. Menu child: 中原傳道團 Index (hidden)
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{ZhongyuanMissionIndexId}', '{ZhongyuanMissionId}', 'ZhongyuanMissionIndex', '/zhongyuan-mission/index',
                    '/zhongyuan-mission/index', NULL,
                    '中原傳道團', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 7. RoleMenu associations (6 entries: 3 catalogs + 3 children)
            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES
                    ('{AdminRoleId}', '{TaiwanSportsFinanceId}'),
                    ('{AdminRoleId}', '{HopeMediaId}'),
                    ('{AdminRoleId}', '{ZhongyuanMissionId}'),
                    ('{AdminRoleId}', '{TaiwanSportsFinanceIndexId}'),
                    ('{AdminRoleId}', '{HopeMediaIndexId}'),
                    ('{AdminRoleId}', '{ZhongyuanMissionIndexId}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete RoleMenus first (FK constraint)
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN (
                    '{TaiwanSportsFinanceId}', '{HopeMediaId}', '{ZhongyuanMissionId}',
                    '{TaiwanSportsFinanceIndexId}', '{HopeMediaIndexId}', '{ZhongyuanMissionIndexId}'
                );
                """);

            // Then delete Menus (children first, then parents)
            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN (
                    '{TaiwanSportsFinanceIndexId}', '{HopeMediaIndexId}', '{ZhongyuanMissionIndexId}',
                    '{TaiwanSportsFinanceId}', '{HopeMediaId}', '{ZhongyuanMissionId}'
                );
                """);
        }
    }
}
