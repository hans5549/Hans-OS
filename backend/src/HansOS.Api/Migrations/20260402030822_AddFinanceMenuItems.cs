using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceMenuItems : Migration
    {
        // 財務記帳 parent catalog
        private const string FinanceBookkeepingId = "b0000001-0000-0000-0000-000000000001";

        // 財務記帳 children
        private const string FinanceIndexId = "b0000001-0000-0000-0000-000000000002";
        private const string FinanceTransactionsId = "b0000001-0000-0000-0000-000000000003";
        private const string FinanceStocksId = "b0000001-0000-0000-0000-000000000004";
        private const string FinanceSettingsId = "b0000001-0000-0000-0000-000000000005";

        // Existing
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        private static readonly string[] AllNewMenuIds =
        [
            FinanceBookkeepingId,
            FinanceIndexId,
            FinanceTransactionsId,
            FinanceStocksId,
            FinanceSettingsId
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 財務記帳 parent catalog
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{FinanceBookkeepingId}', NULL, 'FinanceBookkeeping', '/finance',
                    'BasicLayout', '/finance/transactions',
                    '財務記帳', 'lucide:book-open', 2,
                    false, 1, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 2. Hidden index page
            InsertHiddenMenu(migrationBuilder, FinanceIndexId, FinanceBookkeepingId,
                "FinanceIndex", "/finance/index",
                "/finance/transactions/index",
                "財務記帳");

            // 3. 記帳
            InsertMenu(migrationBuilder, FinanceTransactionsId, FinanceBookkeepingId,
                "FinanceTransactions", "/finance/transactions",
                "/finance/transactions/index",
                "記帳", "lucide:receipt", 1);

            // 4. 投資
            InsertMenu(migrationBuilder, FinanceStocksId, FinanceBookkeepingId,
                "FinanceStocks", "/finance/stocks",
                "/finance/stocks/index",
                "投資", "lucide:trending-up", 2);

            // 5. 設定
            InsertMenu(migrationBuilder, FinanceSettingsId, FinanceBookkeepingId,
                "FinanceSettings", "/finance/settings",
                "/finance/settings/index",
                "設定", "lucide:settings", 3);

            // 6. RoleMenu associations
            var roleMenuValues = string.Join(",\n    ",
                AllNewMenuIds.Select(id => $"('{AdminRoleId}', '{id}')"));

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES
                    {roleMenuValues}
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var idList = string.Join(", ", AllNewMenuIds.Select(id => $"'{id}'"));

            // Delete RoleMenus first (FK constraint)
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({idList});
                """);

            // Delete Menus (children first, then parent)
            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({idList});
                """);
        }

        private static void InsertMenu(
            MigrationBuilder migrationBuilder,
            string id, string parentId,
            string name, string path, string component,
            string title, string icon, int order)
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
            string id, string parentId,
            string name, string path, string component,
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
                    '{title}', NULL, 0,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }
    }
}
