using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFinanceAndMissionMenusAddTodoMenu : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        private const string FinanceBookkeepingId = "b0000001-0000-0000-0000-000000000001";
        private const string FinanceIndexId = "b0000001-0000-0000-0000-000000000002";
        private const string FinanceTransactionsId = "b0000001-0000-0000-0000-000000000003";
        private const string FinanceStocksId = "b0000001-0000-0000-0000-000000000004";
        private const string FinanceSettingsId = "b0000001-0000-0000-0000-000000000005";
        private const string FinanceReportsId = "b0000001-0000-0000-0000-000000000006";

        private const string ZhongyuanMissionId = "d1e2f3a4-0000-0000-0000-000000000006";
        private const string ZhongyuanMissionIndexId = "d1e2f3a4-0000-0000-0000-000000000009";
        private const string ZmActivityRecordsId = "a0000003-0000-0000-0000-000000000001";

        private const string TodoListId = "e0000001-0000-0000-0000-000000000001";
        private const string TodoListIndexId = "e0000001-0000-0000-0000-000000000002";

        private static readonly string[] DisabledMenuIds =
        [
            FinanceBookkeepingId,
            FinanceIndexId,
            FinanceTransactionsId,
            FinanceStocksId,
            FinanceSettingsId,
            FinanceReportsId,
            ZhongyuanMissionId,
            ZhongyuanMissionIndexId,
            ZmActivityRecordsId,
        ];

        private static readonly string[] TodoMenuIds =
        [
            TodoListId,
            TodoListIndexId,
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UpdateMenuIsActive(migrationBuilder, DisabledMenuIds, isActive: false);

            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{TodoListId}', NULL, 'TodoList', '/todo',
                    'BasicLayout', '/todo/index',
                    '代辦清單', 'lucide:list-todo', 3,
                    false, 1, true,
                    false, false, false, true,
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
                    '{TodoListIndexId}', '{TodoListId}', 'TodoListIndex', '/todo/index',
                    '/dashboard/todo/index', NULL,
                    '代辦清單', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES
                    ('{AdminRoleId}', '{TodoListId}'),
                    ('{AdminRoleId}', '{TodoListIndexId}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var todoIdList = string.Join(", ", TodoMenuIds.Select(id => $"'{id}'"));

            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({todoIdList});
                """);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({todoIdList});
                """);

            UpdateMenuIsActive(migrationBuilder, DisabledMenuIds, isActive: true);
        }

        private static void UpdateMenuIsActive(
            MigrationBuilder migrationBuilder,
            IEnumerable<string> menuIds,
            bool isActive)
        {
            var idList = string.Join(", ", menuIds.Select(id => $"'{id}'"));
            var activeValue = isActive ? "true" : "false";

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "IsActive" = {activeValue}
                WHERE "Id" IN ({idList});
                """);
        }
    }
}
