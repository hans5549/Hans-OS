using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFinanceTaskMenus : Migration
    {
        private const string ReceiptTrackingId = "a0000001-0000-0000-0000-000000000006";
        private const string PendingRemittanceId = "a0000001-0000-0000-0000-000000000007";
        private const string TsfTasksId = "a0000001-0000-0000-0000-000000000008";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 隱藏「收據追蹤」和「活動費待匯款」選單（已整合至統一任務清單）
            migrationBuilder.Sql($"""
                UPDATE "Menus" SET "HideInMenu" = true, "IsActive" = false
                WHERE "Id" IN ('{ReceiptTrackingId}', '{PendingRemittanceId}');
                """);

            // 更新「任務清單」→「統一任務清單」
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Title" = '統一任務清單',
                    "Icon" = 'lucide:clipboard-list',
                    "Order" = 6
                WHERE "Id" = '{TsfTasksId}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 還原選單顯示
            migrationBuilder.Sql($"""
                UPDATE "Menus" SET "HideInMenu" = false, "IsActive" = true
                WHERE "Id" IN ('{ReceiptTrackingId}', '{PendingRemittanceId}');
                """);

            // 還原任務清單名稱
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Title" = '任務清單',
                    "Icon" = 'lucide:list-checks',
                    "Order" = 8
                WHERE "Id" = '{TsfTasksId}';
                """);
        }
    }
}
