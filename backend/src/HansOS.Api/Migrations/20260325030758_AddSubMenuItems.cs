using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubMenuItems : Migration
    {
        // Existing parent catalogs
        private const string TaiwanSportsFinanceId = "d1e2f3a4-0000-0000-0000-000000000004";
        private const string HopeMediaId = "d1e2f3a4-0000-0000-0000-000000000005";
        private const string ZhongyuanMissionId = "d1e2f3a4-0000-0000-0000-000000000006";

        // 台灣體育部財務 children
        private const string TsfAnnualBudgetId = "a0000001-0000-0000-0000-000000000001";
        private const string TsfActivitiesId = "a0000001-0000-0000-0000-000000000002";
        private const string TsfShanghaiBankId = "a0000001-0000-0000-0000-000000000003";
        private const string TsfTcbBankId = "a0000001-0000-0000-0000-000000000004";
        private const string TsfSettingsId = "a0000001-0000-0000-0000-000000000005";

        // 主希望光影音部 children
        private const string HmAnnualBudgetId = "a0000002-0000-0000-0000-000000000001";
        private const string HmAnnualPlanId = "a0000002-0000-0000-0000-000000000002";
        private const string HmLearningDocsId = "a0000002-0000-0000-0000-000000000003";
        private const string HmSurveyId = "a0000002-0000-0000-0000-000000000004";

        // 中原傳道團 children
        private const string ZmActivityRecordsId = "a0000003-0000-0000-0000-000000000001";

        // 文章收藏 (independent)
        private const string ArticleCollectionId = "a0000004-0000-0000-0000-000000000001";
        private const string ArticleCollectionIndexId = "a0000004-0000-0000-0000-000000000002";

        // Existing
        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        private static readonly string[] AllNewMenuIds =
        [
            TsfAnnualBudgetId, TsfActivitiesId, TsfShanghaiBankId, TsfTcbBankId, TsfSettingsId,
            HmAnnualBudgetId, HmAnnualPlanId, HmLearningDocsId, HmSurveyId,
            ZmActivityRecordsId,
            ArticleCollectionId, ArticleCollectionIndexId
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Update existing catalogs: show children in sidebar
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = false,
                    "Redirect" = '/taiwan-sports-finance/annual-budget'
                WHERE "Id" = '{TaiwanSportsFinanceId}';
                """);

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = false,
                    "Redirect" = '/hope-media/annual-budget'
                WHERE "Id" = '{HopeMediaId}';
                """);

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = false,
                    "Redirect" = '/zhongyuan-mission/activity-records'
                WHERE "Id" = '{ZhongyuanMissionId}';
                """);

            // 2. 台灣體育部財務 children
            InsertMenu(migrationBuilder, TsfAnnualBudgetId, TaiwanSportsFinanceId,
                "TsfAnnualBudget", "/taiwan-sports-finance/annual-budget",
                "/taiwan-sports-finance/annual-budget/index",
                "年度預算", "lucide:calculator", 2);

            InsertMenu(migrationBuilder, TsfActivitiesId, TaiwanSportsFinanceId,
                "TsfActivities", "/taiwan-sports-finance/activities",
                "/taiwan-sports-finance/activities/index",
                "部門活動", "lucide:calendar-days", 3);

            InsertMenu(migrationBuilder, TsfShanghaiBankId, TaiwanSportsFinanceId,
                "TsfShanghaiBank", "/taiwan-sports-finance/shanghai-bank",
                "/taiwan-sports-finance/shanghai-bank/index",
                "上海銀行收支表", "lucide:landmark", 4);

            InsertMenu(migrationBuilder, TsfTcbBankId, TaiwanSportsFinanceId,
                "TsfTcbBank", "/taiwan-sports-finance/tcb-bank",
                "/taiwan-sports-finance/tcb-bank/index",
                "合作金庫收支表", "lucide:landmark", 5);

            InsertMenu(migrationBuilder, TsfSettingsId, TaiwanSportsFinanceId,
                "TsfSettings", "/taiwan-sports-finance/settings",
                "/taiwan-sports-finance/settings/index",
                "設定", "lucide:settings", 6);

            // 3. 主希望光影音部 children
            InsertMenu(migrationBuilder, HmAnnualBudgetId, HopeMediaId,
                "HmAnnualBudget", "/hope-media/annual-budget",
                "/hope-media/annual-budget/index",
                "年度預算", "lucide:calculator", 2);

            InsertMenu(migrationBuilder, HmAnnualPlanId, HopeMediaId,
                "HmAnnualPlan", "/hope-media/annual-plan",
                "/hope-media/annual-plan/index",
                "年度計畫", "lucide:clipboard-list", 3);

            InsertMenu(migrationBuilder, HmLearningDocsId, HopeMediaId,
                "HmLearningDocs", "/hope-media/learning-docs",
                "/hope-media/learning-docs/index",
                "學習文件", "lucide:book-open", 4);

            InsertMenu(migrationBuilder, HmSurveyId, HopeMediaId,
                "HmSurvey", "/hope-media/survey",
                "/hope-media/survey/index",
                "調查", "lucide:search", 5);

            // 4. 中原傳道團 children
            InsertMenu(migrationBuilder, ZmActivityRecordsId, ZhongyuanMissionId,
                "ZmActivityRecords", "/zhongyuan-mission/activity-records",
                "/zhongyuan-mission/activity-records/index",
                "活動紀錄", "lucide:notebook-pen", 2);

            // 5. 文章收藏 (independent catalog + hidden index)
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{ArticleCollectionId}', NULL, 'ArticleCollection', '/article-collection',
                    'BasicLayout', '/article-collection/index',
                    '文章收藏', 'lucide:bookmark', 4,
                    false, 1, true,
                    false, false, false, true,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            InsertHiddenMenu(migrationBuilder, ArticleCollectionIndexId, ArticleCollectionId,
                "ArticleCollectionIndex", "/article-collection/index",
                "/article-collection/index",
                "文章收藏");

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
            // Delete RoleMenus first (FK constraint)
            var idList = string.Join(", ", AllNewMenuIds.Select(id => $"'{id}'"));

            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({idList});
                """);

            // Delete Menus (children first, then parents)
            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({idList});
                """);

            // Restore catalogs to original state
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = true,
                    "Redirect" = '/taiwan-sports-finance/index'
                WHERE "Id" = '{TaiwanSportsFinanceId}';
                """);

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = true,
                    "Redirect" = '/hope-media/index'
                WHERE "Id" = '{HopeMediaId}';
                """);

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "HideChildrenInMenu" = true,
                    "Redirect" = '/zhongyuan-mission/index'
                WHERE "Id" = '{ZhongyuanMissionId}';
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
                    '{title}', NULL, 1,
                    false, 2, true,
                    true, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }
    }
}
