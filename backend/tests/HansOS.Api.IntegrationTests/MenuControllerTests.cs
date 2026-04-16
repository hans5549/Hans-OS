using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class MenuControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private static readonly Guid PreservedMenuId = Guid.Parse("f1e2d3c4-0000-0000-0000-000000000001");
    private static readonly Guid SystemDesignMenuId = Guid.Parse("c0000001-0000-0000-0000-000000000001");
    private static readonly Guid FundamentalsId = Guid.Parse("c0000002-0000-0000-0000-000000000001");
    private static readonly Guid DesignPatternsId = Guid.Parse("c0000002-0000-0000-0000-000000000002");
    private static readonly Guid CommonTechnologiesId = Guid.Parse("c0000002-0000-0000-0000-000000000003");
    private static readonly Guid OperationsReliabilityId = Guid.Parse("c0000002-0000-0000-0000-000000000004");
    private static readonly Guid RealWorldAppsId = Guid.Parse("c0000002-0000-0000-0000-000000000005");

    private static readonly ItemSeed[] FundamentalsSeeds =
    [
        new(Guid.Parse("c0000002-0000-0000-0000-000000000101"), "NetworkingEssentials", "networking-essentials", 1),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000102"), "ClientServerArchitecture", "client-server-architecture", 2),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000103"), "CapTheorem", "cap-theorem", 3),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000104"), "Scalability", "scalability", 4),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000105"), "ApiDesign", "api-design", 5),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000106"), "ConsistentHashing", "consistent-hashing", 6),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000107"), "DatabaseIndexing", "database-indexing", 7),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000108"), "DatabaseTransactions", "database-transactions", 8),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000109"), "Caching", "caching", 9),
        new(Guid.Parse("c0000002-0000-0000-0000-00000000010a"), "Sharding", "sharding", 10),
        new(Guid.Parse("c0000002-0000-0000-0000-00000000010b"), "Replication", "replication", 11),
        new(Guid.Parse("c0000002-0000-0000-0000-00000000010c"), "NumbersToKnow", "numbers-to-know", 12),
    ];

    private static readonly ItemSeed[] DesignPatternSeeds =
    [
        new(Guid.Parse("c0000002-0000-0000-0000-000000000201"), "ScalingReads", "scaling-reads", 1),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000202"), "ScalingWrites", "scaling-writes", 2),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000203"), "ManageLongRunningTasks", "manage-long-running-tasks", 3),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000204"), "HandlingLargeBlobs", "handling-large-blobs", 4),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000205"), "RealTimeUpdates", "real-time-updates", 5),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000206"), "SearchSystem", "search-system", 6),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000207"), "DataPipelineDesign", "data-pipeline-design", 7),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000208"), "Rag", "rag", 8),
    ];

    private static readonly ItemSeed[] CommonTechnologySeeds =
    [
        new(Guid.Parse("c0000002-0000-0000-0000-000000000301"), "DatabaseTechnology", "database", 1),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000302"), "BlobStorage", "blob-storage", 2),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000303"), "ApiGateway", "api-gateway", 3),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000304"), "LoadBalancer", "load-balancer", 4),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000305"), "ContainerTechnology", "container", 5),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000306"), "Serverless", "serverless", 6),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000307"), "QueueTechnology", "queue", 7),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000308"), "DistributedCache", "distributed-cache", 8),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000309"), "DistributedLock", "distributed-lock", 9),
        new(Guid.Parse("c0000002-0000-0000-0000-00000000030a"), "Cdn", "cdn", 10),
    ];

    private static readonly ItemSeed[] OperationsReliabilitySeeds =
    [
        new(Guid.Parse("c0000002-0000-0000-0000-000000000401"), "DealingWithContention", "dealing-with-contention", 1),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000402"), "OverloadProtection", "overload-protection", 2),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000403"), "ReliableDelivery", "reliable-delivery", 3),
        new(Guid.Parse("c0000002-0000-0000-0000-000000000404"), "Observability", "observability", 4),
    ];

    private static readonly ItemSeed[] RealWorldAppSeeds =
    [
        new(Guid.Parse("c0000001-0000-0000-0000-000000000002"), "QrCodeGenerator", "qr-code-generator", 1),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000003"), "EarthquakeNotification", "earthquake-notification", 2),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000004"), "Polymarket", "polymarket", 3),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000005"), "AmazonPriceTracking", "amazon-price-tracking", 4),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000006"), "TeslaRoboTaxi", "tesla-robo-taxi", 5),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000007"), "SpotifyTrendingSongs", "spotify-trending-songs", 6),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000008"), "Messenger", "messenger", 7),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000009"), "WebhookPlatform", "webhook-platform", 8),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000010"), "GoogleDocs", "google-docs", 9),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000011"), "Youtube", "youtube", 10),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000012"), "ChatgptTasks", "chatgpt-tasks", 11),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000013"), "AirbnbBooking", "airbnb-booking", 12),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000014"), "AgodaAiSupport", "agoda-ai-support", 13),
        new(Guid.Parse("c0000001-0000-0000-0000-000000000015"), "LlmInferenceApi", "llm-inference-api", 14),
    ];

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/menu/all");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authorized_DoesNotReturnRemovedDashboardMenus()
    {
        await SeedPreservedMenuAsync();
        var token = await LoginAndGetTokenAsync();

        var response = await _client.SendAsync(CreateAuthorizedGetRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
        var menus = body.GetProperty("data");
        menus.ValueKind.Should().Be(JsonValueKind.Array);
        ContainsMenuNamed(menus, "FinanceReports").Should().BeTrue();
        ContainsMenuNamed(menus, "Dashboard").Should().BeFalse();
        ContainsMenuNamed(menus, "Analytics").Should().BeFalse();
        ContainsMenuNamed(menus, "Workspace").Should().BeFalse();
        ContainsMenuNamed(menus, "Todo").Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_Authorized_ReturnsReorganizedSystemDesignHierarchy()
    {
        await SeedReorganizedSystemDesignMenusAsync();
        var token = await LoginAndGetTokenAsync();

        var response = await _client.SendAsync(CreateAuthorizedGetRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);

        var menus = body.GetProperty("data");

        TryFindMenuByName(menus, "SystemDesignPractice", out var systemDesignMenu).Should().BeTrue();
        systemDesignMenu.GetProperty("path").GetString().Should().Be("/system-design");
        systemDesignMenu.GetProperty("redirect").GetString().Should().Be("/system-design/fundamentals/networking-essentials");
        systemDesignMenu.GetProperty("children").GetArrayLength().Should().Be(5);

        TryFindChildMenuByName(systemDesignMenu, "Fundamentals", out var fundamentalsMenu).Should().BeTrue();
        fundamentalsMenu.GetProperty("path").GetString().Should().Be("/system-design/fundamentals");
        fundamentalsMenu.GetProperty("redirect").GetString().Should().Be("/system-design/fundamentals/networking-essentials");
        fundamentalsMenu.GetProperty("children").GetArrayLength().Should().Be(12);

        TryFindChildMenuByName(fundamentalsMenu, "NetworkingEssentials", out var networkingEssentialsMenu).Should().BeTrue();
        networkingEssentialsMenu.GetProperty("path").GetString().Should().Be("/system-design/fundamentals/networking-essentials");
        networkingEssentialsMenu.GetProperty("component").GetString().Should().Be("/system-design/fundamentals/networking-essentials/index");

        TryFindChildMenuByName(systemDesignMenu, "DesignPatterns", out var designPatternsMenu).Should().BeTrue();
        designPatternsMenu.GetProperty("children").GetArrayLength().Should().Be(8);

        TryFindChildMenuByName(systemDesignMenu, "CommonTechnologies", out var commonTechnologiesMenu).Should().BeTrue();
        commonTechnologiesMenu.GetProperty("children").GetArrayLength().Should().Be(10);

        TryFindChildMenuByName(systemDesignMenu, "OperationsReliability", out var operationsReliabilityMenu).Should().BeTrue();
        operationsReliabilityMenu.GetProperty("children").GetArrayLength().Should().Be(4);

        TryFindChildMenuByName(systemDesignMenu, "RealWorldApps", out var realWorldAppsMenu).Should().BeTrue();
        realWorldAppsMenu.GetProperty("path").GetString().Should().Be("/system-design/real-world-apps");
        realWorldAppsMenu.GetProperty("redirect").GetString().Should().Be("/system-design/real-world-apps/qr-code-generator");
        realWorldAppsMenu.GetProperty("children").GetArrayLength().Should().Be(14);

        TryFindChildMenuByName(realWorldAppsMenu, "QrCodeGenerator", out var qrCodeGeneratorMenu).Should().BeTrue();
        qrCodeGeneratorMenu.GetProperty("path").GetString().Should().Be("/system-design/real-world-apps/qr-code-generator");
        qrCodeGeneratorMenu.GetProperty("component").GetString().Should().Be("/system-design/real-world-apps/qr-code-generator/index");

        ContainsMenuPath(menus, "/system-design/qr-code-generator").Should().BeFalse();
        ContainsMenuPath(menus, "/system-design/earthquake-notification").Should().BeFalse();
    }

    private async Task SeedReorganizedSystemDesignMenusAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await db.Menus.AnyAsync(menu => menu.Id == SystemDesignMenuId))
        {
            return;
        }

        var adminRoleId = await db.Roles
            .Where(role => role.Name == "admin")
            .Select(role => role.Id)
            .SingleAsync();

        var menus = new List<Menu>
        {
            CreateMenu(
                SystemDesignMenuId,
                null,
                "SystemDesignPractice",
                "/system-design",
                "BasicLayout",
                "/system-design/fundamentals/networking-essentials",
                5,
                MenuType.Catalog),
            CreateMenu(
                FundamentalsId,
                SystemDesignMenuId,
                "Fundamentals",
                "/system-design/fundamentals",
                null,
                "/system-design/fundamentals/networking-essentials",
                1,
                MenuType.Catalog),
            CreateMenu(
                DesignPatternsId,
                SystemDesignMenuId,
                "DesignPatterns",
                "/system-design/design-patterns",
                null,
                "/system-design/design-patterns/scaling-reads",
                2,
                MenuType.Catalog),
            CreateMenu(
                CommonTechnologiesId,
                SystemDesignMenuId,
                "CommonTechnologies",
                "/system-design/common-technologies",
                null,
                "/system-design/common-technologies/database",
                3,
                MenuType.Catalog),
            CreateMenu(
                OperationsReliabilityId,
                SystemDesignMenuId,
                "OperationsReliability",
                "/system-design/operations-reliability",
                null,
                "/system-design/operations-reliability/dealing-with-contention",
                4,
                MenuType.Catalog),
            CreateMenu(
                RealWorldAppsId,
                SystemDesignMenuId,
                "RealWorldApps",
                "/system-design/real-world-apps",
                null,
                "/system-design/real-world-apps/qr-code-generator",
                5,
                MenuType.Catalog),
        };

        menus.AddRange(BuildMenus(FundamentalsId, "/system-design/fundamentals", FundamentalsSeeds));
        menus.AddRange(BuildMenus(DesignPatternsId, "/system-design/design-patterns", DesignPatternSeeds));
        menus.AddRange(BuildMenus(CommonTechnologiesId, "/system-design/common-technologies", CommonTechnologySeeds));
        menus.AddRange(BuildMenus(OperationsReliabilityId, "/system-design/operations-reliability", OperationsReliabilitySeeds));
        menus.AddRange(BuildMenus(RealWorldAppsId, "/system-design/real-world-apps", RealWorldAppSeeds));

        db.Menus.AddRange(menus);
        db.RoleMenus.AddRange(menus.Select(menu => new RoleMenu
        {
            RoleId = adminRoleId,
            MenuId = menu.Id,
        }));

        await db.SaveChangesAsync();
    }

    private async Task SeedPreservedMenuAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var adminRoleId = await db.Roles
            .Where(role => role.Name == "admin")
            .Select(role => role.Id)
            .SingleAsync();

        if (!await db.Menus.AnyAsync(menu => menu.Id == PreservedMenuId))
        {
            db.Menus.Add(new Menu
            {
                Id = PreservedMenuId,
                Name = "FinanceReports",
                Path = "/finance/reports",
                Component = "/finance/reports/index",
                Title = "page.finance.reports",
                Type = MenuType.Menu,
                Order = 1,
            });
        }

        if (!await db.RoleMenus.AnyAsync(roleMenu => roleMenu.RoleId == adminRoleId && roleMenu.MenuId == PreservedMenuId))
        {
            db.RoleMenus.Add(new RoleMenu
            {
                RoleId = adminRoleId,
                MenuId = PreservedMenuId,
            });
        }

        await db.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateAuthorizedGetRequest(string token) =>
        new(HttpMethod.Get, "/menu/all")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
        };

    private async Task<string> LoginAndGetTokenAsync(
        string username = "hans",
        string password = "H@ns19951204")
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);

        var accessToken = body.GetProperty("data").GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();

        return accessToken!;
    }

    private static bool ContainsMenuNamed(JsonElement menus, string name)
    {
        foreach (var menu in menus.EnumerateArray())
        {
            if (menu.GetProperty("name").GetString() == name)
            {
                return true;
            }

            if (menu.TryGetProperty("children", out var children) &&
                children.ValueKind == JsonValueKind.Array &&
                ContainsMenuNamed(children, name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsMenuPath(JsonElement menus, string path)
    {
        foreach (var menu in menus.EnumerateArray())
        {
            if (menu.GetProperty("path").GetString() == path)
            {
                return true;
            }

            if (menu.TryGetProperty("children", out var children) &&
                children.ValueKind == JsonValueKind.Array &&
                ContainsMenuPath(children, path))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindMenuByName(JsonElement menus, string name, out JsonElement foundMenu)
    {
        foreach (var menu in menus.EnumerateArray())
        {
            if (menu.GetProperty("name").GetString() == name)
            {
                foundMenu = menu;
                return true;
            }

            if (menu.TryGetProperty("children", out var children) &&
                children.ValueKind == JsonValueKind.Array &&
                TryFindMenuByName(children, name, out foundMenu))
            {
                return true;
            }
        }

        foundMenu = default;
        return false;
    }

    private static bool TryFindChildMenuByName(JsonElement menu, string name, out JsonElement foundMenu)
    {
        if (!menu.TryGetProperty("children", out var children) ||
            children.ValueKind != JsonValueKind.Array)
        {
            foundMenu = default;
            return false;
        }

        return TryFindMenuByName(children, name, out foundMenu);
    }

    private static IEnumerable<Menu> BuildMenus(
        Guid parentId,
        string categoryBasePath,
        IEnumerable<ItemSeed> seeds)
    {
        return seeds.Select(seed => CreateMenu(
            seed.Id,
            parentId,
            seed.Name,
            $"{categoryBasePath}/{seed.Slug}",
            $"{categoryBasePath}/{seed.Slug}/index",
            null,
            seed.Order,
            MenuType.Menu));
    }

    private static Menu CreateMenu(
        Guid id,
        Guid? parentId,
        string name,
        string path,
        string? component,
        string? redirect,
        int order,
        MenuType type)
    {
        return new Menu
        {
            Id = id,
            ParentId = parentId,
            Name = name,
            Path = path,
            Component = component,
            Redirect = redirect,
            Title = name,
            Order = order,
            Type = type,
            IsActive = true,
        };
    }

    private sealed record ItemSeed(Guid Id, string Name, string Slug, int Order);
}
