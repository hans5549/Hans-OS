using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedEssentialData : Migration
    {
        // Fixed IDs for deterministic seeding
        private const string AdminRoleId = "a1b2c3d4-0001-0001-0001-000000000001";
        private const string HansUserId = "b2c3d4e5-0002-0002-0002-000000000002";
        private const string DashboardMenuId = "c3d4e5f6-0003-0003-0003-000000000003";
        private const string AnalyticsMenuId = "d4e5f6a7-0004-0004-0004-000000000004";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Admin Role ──────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: ["Id", "Name", "NormalizedName", "ConcurrencyStamp"],
                values: new object[] { AdminRoleId, "admin", "ADMIN", Guid.NewGuid().ToString() });

            // ── 2. Hans User ───────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: [
                    "Id", "UserName", "NormalizedUserName",
                    "Email", "NormalizedEmail", "EmailConfirmed",
                    "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "AccessFailedCount", "Avatar", "HomePath", "IsActive", "RealName"
                ],
                values: new object[] {
                    HansUserId, "hans", "HANS",
                    "hans@hansos.dev", "HANS@HANSOS.DEV", true,
                    "AQAAAAIAAYagAAAAEHHPkZxg2PX+DM7eDtMrk+UsWvYsnQ0Relp/xvvbv9eEO1UNc1WvfkkKiO9N4HsKHQ==",
                    Guid.NewGuid().ToString(), Guid.NewGuid().ToString(),
                    false, false, false,
                    0, "", "/analytics", true, "Hans"
                });

            // ── 3. Assign admin role to hans ────────────────────────────────────
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: ["UserId", "RoleId"],
                values: new object[] { HansUserId, AdminRoleId });

            // ── 4. Dashboard menu (parent — layout wrapper) ─────────────────────
            migrationBuilder.InsertData(
                table: "Menus",
                columns: [
                    "Id", "Name", "Path", "ComponentKey", "TitleKey",
                    "Icon", "OrderNo", "Type", "Status",
                    "ParentId", "Redirect",
                    "AffixTab", "HideInBreadcrumb", "HideInMenu", "HideInTab",
                    "KeepAlive", "MenuVisibleWithForbidden"
                ],
                values: new object[] {
                    Guid.Parse(DashboardMenuId), "Dashboard", "/dashboard", "BasicLayout", "Dashboard",
                    "lucide:layout-dashboard", 1, "Catalog", true,
                    null!, "/dashboard/analytics",
                    false, false, false, false,
                    false, false
                });

            // ── 5. Analytics page (child of Dashboard) ──────────────────────────
            migrationBuilder.InsertData(
                table: "Menus",
                columns: [
                    "Id", "Name", "Path", "ComponentKey", "TitleKey",
                    "Icon", "OrderNo", "Type", "Status",
                    "ParentId",
                    "AffixTab", "HideInBreadcrumb", "HideInMenu", "HideInTab",
                    "KeepAlive", "MenuVisibleWithForbidden"
                ],
                values: new object[] {
                    Guid.Parse(AnalyticsMenuId), "Analytics", "/dashboard/analytics",
                    "/dashboard/analytics/index", "Analytics",
                    "lucide:area-chart", 1, "Menu", true,
                    Guid.Parse(DashboardMenuId),
                    true, false, false, false,
                    false, false
                });

            // ── 6. Assign menus to admin role ───────────────────────────────────
            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: ["RoleId", "MenuId"],
                values: new object[] { AdminRoleId, Guid.Parse(DashboardMenuId) });

            migrationBuilder.InsertData(
                table: "RoleMenus",
                columns: ["RoleId", "MenuId"],
                values: new object[] { AdminRoleId, Guid.Parse(AnalyticsMenuId) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "RoleMenus", keyColumns: ["RoleId", "MenuId"], keyValues: new object[] { AdminRoleId, Guid.Parse(AnalyticsMenuId) });
            migrationBuilder.DeleteData(table: "RoleMenus", keyColumns: ["RoleId", "MenuId"], keyValues: new object[] { AdminRoleId, Guid.Parse(DashboardMenuId) });
            migrationBuilder.DeleteData(table: "Menus", keyColumn: "Id", keyValue: Guid.Parse(AnalyticsMenuId));
            migrationBuilder.DeleteData(table: "Menus", keyColumn: "Id", keyValue: Guid.Parse(DashboardMenuId));
            migrationBuilder.DeleteData(table: "AspNetUserRoles", keyColumns: ["UserId", "RoleId"], keyValues: new object[] { HansUserId, AdminRoleId });
            migrationBuilder.DeleteData(table: "AspNetUsers", keyColumn: "Id", keyValue: HansUserId);
            migrationBuilder.DeleteData(table: "AspNetRoles", keyColumn: "Id", keyValue: AdminRoleId);
        }
    }
}
