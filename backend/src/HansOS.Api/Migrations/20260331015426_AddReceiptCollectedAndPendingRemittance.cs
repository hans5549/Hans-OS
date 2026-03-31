using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptCollectedAndPendingRemittance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceiptCollected",
                table: "BankTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PendingRemittances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRemittances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingRemittances_SportsDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "SportsDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingRemittances_DepartmentId",
                table: "PendingRemittances",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRemittances_Status",
                table: "PendingRemittances",
                column: "Status");

            // ── 新增選單項目 ──────────────────────────────────
            var tsfParentId = "d1e2f3a4-0000-0000-0000-000000000004";
            var adminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
            var receiptTrackingId = "a0000001-0000-0000-0000-000000000006";
            var pendingRemittanceId = "a0000001-0000-0000-0000-000000000007";

            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{receiptTrackingId}', '{tsfParentId}', 'TsfReceiptTracking',
                    '/taiwan-sports-finance/receipt-tracking',
                    '/taiwan-sports-finance/receipt-tracking/index', NULL,
                    '收據追蹤', 'lucide:receipt', 6,
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
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{pendingRemittanceId}', '{tsfParentId}', 'TsfPendingRemittance',
                    '/taiwan-sports-finance/pending-remittance',
                    '/taiwan-sports-finance/pending-remittance/index', NULL,
                    '活動費待匯款', 'lucide:send', 7,
                    false, 2, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // Update Settings menu order to be after new items
            migrationBuilder.Sql($"""
                UPDATE "Menus" SET "Order" = 8
                WHERE "Id" = 'a0000001-0000-0000-0000-000000000005';
                """);

            // Assign to admin role
            migrationBuilder.Sql($"""
                INSERT INTO "RoleMenus" ("RoleId", "MenuId")
                VALUES
                    ('{adminRoleId}', '{receiptTrackingId}'),
                    ('{adminRoleId}', '{pendingRemittanceId}')
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove role-menu associations
            migrationBuilder.Sql("""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN (
                    'a0000001-0000-0000-0000-000000000006',
                    'a0000001-0000-0000-0000-000000000007'
                );
                """);

            // Remove menu items
            migrationBuilder.Sql("""
                DELETE FROM "Menus"
                WHERE "Id" IN (
                    'a0000001-0000-0000-0000-000000000006',
                    'a0000001-0000-0000-0000-000000000007'
                );
                """);

            // Restore Settings order
            migrationBuilder.Sql("""
                UPDATE "Menus" SET "Order" = 6
                WHERE "Id" = 'a0000001-0000-0000-0000-000000000005';
                """);

            migrationBuilder.DropTable(
                name: "PendingRemittances");

            migrationBuilder.DropColumn(
                name: "ReceiptCollected",
                table: "BankTransactions");
        }
    }
}
