using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTsfTasksMenu : Migration
    {
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
        private const string TsfParentId = "d1e2f3a4-0000-0000-0000-000000000004";
        private const string TsfTasksId = "a0000001-0000-0000-0000-000000000008";
        private const string TsfSettingsId = "a0000001-0000-0000-0000-000000000005";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 新增「任務清單」選單項目
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{TsfTasksId}', '{TsfParentId}', 'TsfTasks',
                    '/taiwan-sports-finance/tasks',
                    '/taiwan-sports-finance/tasks/index', NULL,
                    '任務清單', 'lucide:list-checks', 8,
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 將「設定」的排序從 8 調整為 9
            migrationBuilder.Sql($"""
                UPDATE "Menus" SET "Order" = 9
                WHERE "Id" = '{TsfSettingsId}';
                """);

            // 指派給管理者角色
            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES ('{AdminRoleId}', '{TsfTasksId}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 刪除角色選單關聯
            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" = '{TsfTasksId}';
                """);

            // 刪除選單項目
            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" = '{TsfTasksId}';
                """);

            // 還原「設定」的排序
            migrationBuilder.Sql($"""
                UPDATE "Menus" SET "Order" = 8
                WHERE "Id" = '{TsfSettingsId}';
                """);
        }
    }
}
