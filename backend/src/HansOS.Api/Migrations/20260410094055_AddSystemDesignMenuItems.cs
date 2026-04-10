using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemDesignMenuItems : Migration
    {
        // 現代系統設計實作練習 父選單
        private const string SystemDesignParentId = "c0000001-0000-0000-0000-000000000001";

        // 子選單
        private const string QrCodeGeneratorId       = "c0000001-0000-0000-0000-000000000002";
        private const string EarthquakeNotificationId = "c0000001-0000-0000-0000-000000000003";
        private const string PolymarketId            = "c0000001-0000-0000-0000-000000000004";
        private const string AmazonPriceTrackingId   = "c0000001-0000-0000-0000-000000000005";
        private const string TeslaRoboTaxiId         = "c0000001-0000-0000-0000-000000000006";
        private const string SpotifyTrendingSongsId  = "c0000001-0000-0000-0000-000000000007";
        private const string MessengerId             = "c0000001-0000-0000-0000-000000000008";
        private const string WebhookPlatformId       = "c0000001-0000-0000-0000-000000000009";
        private const string GoogleDocsId            = "c0000001-0000-0000-0000-000000000010";
        private const string YoutubeId               = "c0000001-0000-0000-0000-000000000011";
        private const string ChatgptTasksId          = "c0000001-0000-0000-0000-000000000012";
        private const string AirbnbBookingId         = "c0000001-0000-0000-0000-000000000013";
        private const string AgodaAiSupportId        = "c0000001-0000-0000-0000-000000000014";
        private const string LlmInferenceApiId       = "c0000001-0000-0000-0000-000000000015";

        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        private static readonly string[] AllNewMenuIds =
        [
            SystemDesignParentId,
            QrCodeGeneratorId,
            EarthquakeNotificationId,
            PolymarketId,
            AmazonPriceTrackingId,
            TeslaRoboTaxiId,
            SpotifyTrendingSongsId,
            MessengerId,
            WebhookPlatformId,
            GoogleDocsId,
            YoutubeId,
            ChatgptTasksId,
            AirbnbBookingId,
            AgodaAiSupportId,
            LlmInferenceApiId,
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 父選單
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    '{SystemDesignParentId}', NULL, 'SystemDesignPractice', '/system-design',
                    'BasicLayout', '/system-design/qr-code-generator',
                    '現代系統設計實作練習', 'lucide:server', 5,
                    false, 1, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);

            // 2. 子選單
            InsertMenu(migrationBuilder, QrCodeGeneratorId,       SystemDesignParentId, "QrCodeGenerator",       "/system-design/qr-code-generator",       "/system-design/qr-code-generator/index",       "QR Code 生成器",   "lucide:qr-code",      1);
            InsertMenu(migrationBuilder, EarthquakeNotificationId, SystemDesignParentId, "EarthquakeNotification","/system-design/earthquake-notification",  "/system-design/earthquake-notification/index", "地震預警系統",     "lucide:bell",         2);
            InsertMenu(migrationBuilder, PolymarketId,            SystemDesignParentId, "Polymarket",            "/system-design/polymarket",               "/system-design/polymarket/index",              "預測市場平台",     "lucide:trending-up",  3);
            InsertMenu(migrationBuilder, AmazonPriceTrackingId,   SystemDesignParentId, "AmazonPriceTracking",   "/system-design/amazon-price-tracking",    "/system-design/amazon-price-tracking/index",   "電商價格追蹤",     "lucide:tag",          4);
            InsertMenu(migrationBuilder, TeslaRoboTaxiId,         SystemDesignParentId, "TeslaRoboTaxi",         "/system-design/tesla-robo-taxi",          "/system-design/tesla-robo-taxi/index",         "自動駕駛叫車系統", "lucide:car",          5);
            InsertMenu(migrationBuilder, SpotifyTrendingSongsId,  SystemDesignParentId, "SpotifyTrendingSongs",  "/system-design/spotify-trending-songs",   "/system-design/spotify-trending-songs/index",  "音樂排行榜系統",   "lucide:music",        6);
            InsertMenu(migrationBuilder, MessengerId,             SystemDesignParentId, "Messenger",             "/system-design/messenger",                "/system-design/messenger/index",               "即時通訊系統",     "lucide:message-circle",7);
            InsertMenu(migrationBuilder, WebhookPlatformId,       SystemDesignParentId, "WebhookPlatform",       "/system-design/webhook-platform",         "/system-design/webhook-platform/index",        "Webhook 平台",     "lucide:webhook",      8);
            InsertMenu(migrationBuilder, GoogleDocsId,            SystemDesignParentId, "GoogleDocs",            "/system-design/google-docs",              "/system-design/google-docs/index",             "協作文檔編輯",     "lucide:file-text",    9);
            InsertMenu(migrationBuilder, YoutubeId,               SystemDesignParentId, "Youtube",               "/system-design/youtube",                  "/system-design/youtube/index",                 "影音平台架構",     "lucide:video",        10);
            InsertMenu(migrationBuilder, ChatgptTasksId,          SystemDesignParentId, "ChatgptTasks",          "/system-design/chatgpt-tasks",            "/system-design/chatgpt-tasks/index",           "AI 任務系統",      "lucide:bot",          11);
            InsertMenu(migrationBuilder, AirbnbBookingId,         SystemDesignParentId, "AirbnbBooking",         "/system-design/airbnb-booking",           "/system-design/airbnb-booking/index",          "短租平台架構",     "lucide:home",         12);
            InsertMenu(migrationBuilder, AgodaAiSupportId,        SystemDesignParentId, "AgodaAiSupport",        "/system-design/agoda-ai-support",         "/system-design/agoda-ai-support/index",        "旅遊平台客服",     "lucide:headphones",   13);
            InsertMenu(migrationBuilder, LlmInferenceApiId,       SystemDesignParentId, "LlmInferenceApi",       "/system-design/llm-inference-api",        "/system-design/llm-inference-api/index",       "LLM 推論服務",     "lucide:cpu",          14);

            // 3. RoleMenu 關聯
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

            migrationBuilder.Sql($"""
                DELETE FROM "RoleMenus"
                WHERE "MenuId" IN ({idList});
                """);

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
    }
}
