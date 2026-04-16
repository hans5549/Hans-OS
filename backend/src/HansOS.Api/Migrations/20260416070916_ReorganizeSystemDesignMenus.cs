using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HansOS.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReorganizeSystemDesignMenus : Migration
    {
        private const string SystemDesignParentId = "c0000001-0000-0000-0000-000000000001";

        private const string QrCodeGeneratorId = "c0000001-0000-0000-0000-000000000002";
        private const string EarthquakeNotificationId = "c0000001-0000-0000-0000-000000000003";
        private const string PolymarketId = "c0000001-0000-0000-0000-000000000004";
        private const string AmazonPriceTrackingId = "c0000001-0000-0000-0000-000000000005";
        private const string TeslaRoboTaxiId = "c0000001-0000-0000-0000-000000000006";
        private const string SpotifyTrendingSongsId = "c0000001-0000-0000-0000-000000000007";
        private const string MessengerId = "c0000001-0000-0000-0000-000000000008";
        private const string WebhookPlatformId = "c0000001-0000-0000-0000-000000000009";
        private const string GoogleDocsId = "c0000001-0000-0000-0000-000000000010";
        private const string YoutubeId = "c0000001-0000-0000-0000-000000000011";
        private const string ChatgptTasksId = "c0000001-0000-0000-0000-000000000012";
        private const string AirbnbBookingId = "c0000001-0000-0000-0000-000000000013";
        private const string AgodaAiSupportId = "c0000001-0000-0000-0000-000000000014";
        private const string LlmInferenceApiId = "c0000001-0000-0000-0000-000000000015";

        private const string FundamentalsId = "c0000002-0000-0000-0000-000000000001";
        private const string DesignPatternsId = "c0000002-0000-0000-0000-000000000002";
        private const string CommonTechnologiesId = "c0000002-0000-0000-0000-000000000003";
        private const string OperationsReliabilityId = "c0000002-0000-0000-0000-000000000004";
        private const string RealWorldAppsId = "c0000002-0000-0000-0000-000000000005";

        private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";

        private static readonly MenuDefinition[] CategoryMenus =
        [
            new(FundamentalsId, SystemDesignParentId, "Fundamentals", "/system-design/fundamentals", null,
                "/system-design/fundamentals/networking-essentials", "基本觀念", "lucide:book-open", 1, 1),
            new(DesignPatternsId, SystemDesignParentId, "DesignPatterns", "/system-design/design-patterns", null,
                "/system-design/design-patterns/scaling-reads", "設計模式", "lucide:blocks", 2, 1),
            new(CommonTechnologiesId, SystemDesignParentId, "CommonTechnologies", "/system-design/common-technologies", null,
                "/system-design/common-technologies/database", "常用技術", "lucide:wrench", 3, 1),
            new(OperationsReliabilityId, SystemDesignParentId, "OperationsReliability", "/system-design/operations-reliability", null,
                "/system-design/operations-reliability/dealing-with-contention", "維運與可靠性", "lucide:shield-check", 4, 1),
            new(RealWorldAppsId, SystemDesignParentId, "RealWorldApps", "/system-design/real-world-apps", null,
                "/system-design/real-world-apps/qr-code-generator", "真實大型應用設計", "lucide:building", 5, 1),
        ];

        private static readonly MenuDefinition[] LearningMenus =
        [
            new("c0000002-0000-0000-0000-000000000101", FundamentalsId, "NetworkingEssentials", "/system-design/fundamentals/networking-essentials",
                "/system-design/fundamentals/networking-essentials/index", null, "Networking Essentials | 網路基本原理", "lucide:network", 1, 2),
            new("c0000002-0000-0000-0000-000000000102", FundamentalsId, "ClientServerArchitecture", "/system-design/fundamentals/client-server-architecture",
                "/system-design/fundamentals/client-server-architecture/index", null, "Client-Server Architecture | 客戶端-伺服器架構", "lucide:server", 2, 2),
            new("c0000002-0000-0000-0000-000000000103", FundamentalsId, "CapTheorem", "/system-design/fundamentals/cap-theorem",
                "/system-design/fundamentals/cap-theorem/index", null, "CAP Theorem | CAP 定理", "lucide:triangle", 3, 2),
            new("c0000002-0000-0000-0000-000000000104", FundamentalsId, "Scalability", "/system-design/fundamentals/scalability",
                "/system-design/fundamentals/scalability/index", null, "Scalability | 擴展性", "lucide:move-up-right", 4, 2),
            new("c0000002-0000-0000-0000-000000000105", FundamentalsId, "ApiDesign", "/system-design/fundamentals/api-design",
                "/system-design/fundamentals/api-design/index", null, "API Design | API 設計", "lucide:plug", 5, 2),
            new("c0000002-0000-0000-0000-000000000106", FundamentalsId, "ConsistentHashing", "/system-design/fundamentals/consistent-hashing",
                "/system-design/fundamentals/consistent-hashing/index", null, "Consistent Hashing | 一致性雜湊", "lucide:hash", 6, 2),
            new("c0000002-0000-0000-0000-000000000107", FundamentalsId, "DatabaseIndexing", "/system-design/fundamentals/database-indexing",
                "/system-design/fundamentals/database-indexing/index", null, "Database Indexing | 資料庫索引", "lucide:database", 7, 2),
            new("c0000002-0000-0000-0000-000000000108", FundamentalsId, "DatabaseTransactions", "/system-design/fundamentals/database-transactions",
                "/system-design/fundamentals/database-transactions/index", null, "Database Transactions | 資料庫交易", "lucide:arrow-left-right", 8, 2),
            new("c0000002-0000-0000-0000-000000000109", FundamentalsId, "Caching", "/system-design/fundamentals/caching",
                "/system-design/fundamentals/caching/index", null, "Caching | 快取", "lucide:hard-drive", 9, 2),
            new("c0000002-0000-0000-0000-00000000010a", FundamentalsId, "Sharding", "/system-design/fundamentals/sharding",
                "/system-design/fundamentals/sharding/index", null, "Sharding | 分片", "lucide:rows-3", 10, 2),
            new("c0000002-0000-0000-0000-00000000010b", FundamentalsId, "Replication", "/system-design/fundamentals/replication",
                "/system-design/fundamentals/replication/index", null, "Replication | 複寫", "lucide:copy", 11, 2),
            new("c0000002-0000-0000-0000-00000000010c", FundamentalsId, "NumbersToKnow", "/system-design/fundamentals/numbers-to-know",
                "/system-design/fundamentals/numbers-to-know/index", null, "Numbers to Know | 數字概念", "lucide:calculator", 12, 2),

            new("c0000002-0000-0000-0000-000000000201", DesignPatternsId, "ScalingReads", "/system-design/design-patterns/scaling-reads",
                "/system-design/design-patterns/scaling-reads/index", null, "Scaling Reads | 擴展讀取效能", "lucide:book-open-check", 1, 2),
            new("c0000002-0000-0000-0000-000000000202", DesignPatternsId, "ScalingWrites", "/system-design/design-patterns/scaling-writes",
                "/system-design/design-patterns/scaling-writes/index", null, "Scaling Writes | 擴展寫入效能", "lucide:pen-line", 2, 2),
            new("c0000002-0000-0000-0000-000000000203", DesignPatternsId, "ManageLongRunningTasks", "/system-design/design-patterns/manage-long-running-tasks",
                "/system-design/design-patterns/manage-long-running-tasks/index", null, "Manage Long Running Tasks | 管理長時間執行任務", "lucide:clock", 3, 2),
            new("c0000002-0000-0000-0000-000000000204", DesignPatternsId, "HandlingLargeBlobs", "/system-design/design-patterns/handling-large-blobs",
                "/system-design/design-patterns/handling-large-blobs/index", null, "Handling Large Blobs | 處理大型檔案", "lucide:file-archive", 4, 2),
            new("c0000002-0000-0000-0000-000000000205", DesignPatternsId, "RealTimeUpdates", "/system-design/design-patterns/real-time-updates",
                "/system-design/design-patterns/real-time-updates/index", null, "Real-time Updates | 即時更新", "lucide:zap", 5, 2),
            new("c0000002-0000-0000-0000-000000000206", DesignPatternsId, "SearchSystem", "/system-design/design-patterns/search-system",
                "/system-design/design-patterns/search-system/index", null, "Search System | 搜尋系統", "lucide:search", 6, 2),
            new("c0000002-0000-0000-0000-000000000207", DesignPatternsId, "DataPipelineDesign", "/system-design/design-patterns/data-pipeline-design",
                "/system-design/design-patterns/data-pipeline-design/index", null, "Data Pipeline Design | 資料管線", "lucide:git-branch", 7, 2),
            new("c0000002-0000-0000-0000-000000000208", DesignPatternsId, "Rag", "/system-design/design-patterns/rag",
                "/system-design/design-patterns/rag/index", null, "RAG (Retrival-Augmented Generation) | 檢索增強生成", "lucide:brain", 8, 2),

            new("c0000002-0000-0000-0000-000000000301", CommonTechnologiesId, "DatabaseTechnology", "/system-design/common-technologies/database",
                "/system-design/common-technologies/database/index", null, "Database | 資料庫", "lucide:database", 1, 2),
            new("c0000002-0000-0000-0000-000000000302", CommonTechnologiesId, "BlobStorage", "/system-design/common-technologies/blob-storage",
                "/system-design/common-technologies/blob-storage/index", null, "Blob Storage | 物件/大型二進位儲存", "lucide:package", 2, 2),
            new("c0000002-0000-0000-0000-000000000303", CommonTechnologiesId, "ApiGateway", "/system-design/common-technologies/api-gateway",
                "/system-design/common-technologies/api-gateway/index", null, "API Gateway | API 閘道", "lucide:router", 3, 2),
            new("c0000002-0000-0000-0000-000000000304", CommonTechnologiesId, "LoadBalancer", "/system-design/common-technologies/load-balancer",
                "/system-design/common-technologies/load-balancer/index", null, "Load Balancer | 負載平衡器", "lucide:scale", 4, 2),
            new("c0000002-0000-0000-0000-000000000305", CommonTechnologiesId, "ContainerTechnology", "/system-design/common-technologies/container",
                "/system-design/common-technologies/container/index", null, "Container | 容器化技術", "lucide:box", 5, 2),
            new("c0000002-0000-0000-0000-000000000306", CommonTechnologiesId, "Serverless", "/system-design/common-technologies/serverless",
                "/system-design/common-technologies/serverless/index", null, "Serverless | 無伺服器架構", "lucide:cloud", 6, 2),
            new("c0000002-0000-0000-0000-000000000307", CommonTechnologiesId, "QueueTechnology", "/system-design/common-technologies/queue",
                "/system-design/common-technologies/queue/index", null, "Queue | 訊息佇列", "lucide:list-ordered", 7, 2),
            new("c0000002-0000-0000-0000-000000000308", CommonTechnologiesId, "DistributedCache", "/system-design/common-technologies/distributed-cache",
                "/system-design/common-technologies/distributed-cache/index", null, "Distributed Cache | 分散式快取", "lucide:layers", 8, 2),
            new("c0000002-0000-0000-0000-000000000309", CommonTechnologiesId, "DistributedLock", "/system-design/common-technologies/distributed-lock",
                "/system-design/common-technologies/distributed-lock/index", null, "Distributed Lock | 分散式鎖", "lucide:lock", 9, 2),
            new("c0000002-0000-0000-0000-00000000030a", CommonTechnologiesId, "Cdn", "/system-design/common-technologies/cdn",
                "/system-design/common-technologies/cdn/index", null, "CDN (Content Delivery Network) | 內容傳遞網路", "lucide:globe", 10, 2),

            new("c0000002-0000-0000-0000-000000000401", OperationsReliabilityId, "DealingWithContention", "/system-design/operations-reliability/dealing-with-contention",
                "/system-design/operations-reliability/dealing-with-contention/index", null, "Dealing with Contention | 處理資源競爭", "lucide:swords", 1, 2),
            new("c0000002-0000-0000-0000-000000000402", OperationsReliabilityId, "OverloadProtection", "/system-design/operations-reliability/overload-protection",
                "/system-design/operations-reliability/overload-protection/index", null, "Overload Protection | 過載保護", "lucide:shield", 2, 2),
            new("c0000002-0000-0000-0000-000000000403", OperationsReliabilityId, "ReliableDelivery", "/system-design/operations-reliability/reliable-delivery",
                "/system-design/operations-reliability/reliable-delivery/index", null, "Reliable Delivery | 可靠訊息傳遞", "lucide:check-check", 3, 2),
            new("c0000002-0000-0000-0000-000000000404", OperationsReliabilityId, "Observability", "/system-design/operations-reliability/observability",
                "/system-design/operations-reliability/observability/index", null, "Observability | 可觀測性", "lucide:eye", 4, 2),
        ];

        private static readonly ExistingMenuUpdate[] ExistingRealWorldMenus =
        [
            new(QrCodeGeneratorId, "qr-code-generator"),
            new(EarthquakeNotificationId, "earthquake-notification"),
            new(PolymarketId, "polymarket"),
            new(AmazonPriceTrackingId, "amazon-price-tracking"),
            new(TeslaRoboTaxiId, "tesla-robo-taxi"),
            new(SpotifyTrendingSongsId, "spotify-trending-songs"),
            new(MessengerId, "messenger"),
            new(WebhookPlatformId, "webhook-platform"),
            new(GoogleDocsId, "google-docs"),
            new(YoutubeId, "youtube"),
            new(ChatgptTasksId, "chatgpt-tasks"),
            new(AirbnbBookingId, "airbnb-booking"),
            new(AgodaAiSupportId, "agoda-ai-support"),
            new(LlmInferenceApiId, "llm-inference-api"),
        ];

        private static readonly HomePathRedirect[] LegacyHomePathRedirects =
            [.. ExistingRealWorldMenus.Select(menu => new HomePathRedirect(
                $"/system-design/{menu.Slug}",
                $"/system-design/real-world-apps/{menu.Slug}"))];

        private static readonly string[] AllNewMenuIds =
            [.. CategoryMenus.Select(menu => menu.Id), .. LearningMenus.Select(menu => menu.Id)];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var categoryMenu in CategoryMenus)
            {
                InsertMenu(migrationBuilder, categoryMenu);
            }

            foreach (var learningMenu in LearningMenus)
            {
                InsertMenu(migrationBuilder, learningMenu);
            }

            foreach (var existingRealWorldMenu in ExistingRealWorldMenus)
            {
                UpdateExistingMenu(
                    migrationBuilder,
                    existingRealWorldMenu.Id,
                    RealWorldAppsId,
                    $"/system-design/real-world-apps/{existingRealWorldMenu.Slug}",
                    $"/system-design/real-world-apps/{existingRealWorldMenu.Slug}/index");
            }

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Redirect" = '/system-design/fundamentals/networking-essentials'
                WHERE "Id" = '{SystemDesignParentId}';
                """);

            UpdateUserHomePaths(migrationBuilder, LegacyHomePathRedirects, updateToNewPaths: true);

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

            foreach (var existingRealWorldMenu in ExistingRealWorldMenus)
            {
                UpdateExistingMenu(
                    migrationBuilder,
                    existingRealWorldMenu.Id,
                    SystemDesignParentId,
                    $"/system-design/{existingRealWorldMenu.Slug}",
                    $"/system-design/{existingRealWorldMenu.Slug}/index");
            }

            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "Redirect" = '/system-design/qr-code-generator'
                WHERE "Id" = '{SystemDesignParentId}';
                """);

            UpdateUserHomePaths(migrationBuilder, LegacyHomePathRedirects, updateToNewPaths: false);

            migrationBuilder.Sql($"""
                DELETE FROM "Menus"
                WHERE "Id" IN ({idList});
                """);
        }

        private static void InsertMenu(MigrationBuilder migrationBuilder, MenuDefinition menu)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "Menus" (
                    "Id", "ParentId", "Name", "Path", "Component", "Redirect",
                    "Title", "Icon", "Order", "AffixTab", "Type", "IsActive",
                    "HideInMenu", "HideInTab", "HideInBreadcrumb", "HideChildrenInMenu",
                    "KeepAlive", "NoBasicLayout", "ActiveIcon"
                ) VALUES (
                    {ToSqlValue(menu.Id)}, {ToSqlValue(menu.ParentId)}, {ToSqlValue(menu.Name)}, {ToSqlValue(menu.Path)},
                    {ToSqlValue(menu.Component)}, {ToSqlValue(menu.Redirect)},
                    {ToSqlValue(menu.Title)}, {ToSqlValue(menu.Icon)}, {menu.Order},
                    false, {menu.Type}, true,
                    false, false, false, false,
                    false, false, NULL
                ) ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        private static void UpdateExistingMenu(
            MigrationBuilder migrationBuilder,
            string id,
            string parentId,
            string path,
            string component)
        {
            migrationBuilder.Sql($"""
                UPDATE "Menus"
                SET "ParentId" = {ToSqlValue(parentId)},
                    "Path" = {ToSqlValue(path)},
                    "Component" = {ToSqlValue(component)}
                WHERE "Id" = {ToSqlValue(id)};
                """);
        }

        private static string ToSqlValue(string? value)
        {
            if (value is null)
            {
                return "NULL";
            }

            return $"'{value.Replace("'", "''")}'";
        }

        private static void UpdateUserHomePaths(
            MigrationBuilder migrationBuilder,
            IEnumerable<HomePathRedirect> redirects,
            bool updateToNewPaths)
        {
            foreach (var redirect in redirects)
            {
                var sourcePath = updateToNewPaths ? redirect.LegacyPath : redirect.NewPath;
                var targetPath = updateToNewPaths ? redirect.NewPath : redirect.LegacyPath;

                migrationBuilder.Sql($"""
                    UPDATE "AspNetUsers"
                    SET "HomePath" = {ToSqlValue(targetPath)}
                    WHERE "HomePath" = {ToSqlValue(sourcePath)};
                    """);
            }
        }

        private sealed record MenuDefinition(
            string Id,
            string ParentId,
            string Name,
            string Path,
            string? Component,
            string? Redirect,
            string Title,
            string Icon,
            int Order,
            int Type);

        private sealed record ExistingMenuUpdate(string Id, string Slug);

        private sealed record HomePathRedirect(string LegacyPath, string NewPath);
    }
}
