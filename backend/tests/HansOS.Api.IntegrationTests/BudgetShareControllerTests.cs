using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

/// <summary>部門分享管理 (admin) + 公開端 (public) 整合測試</summary>
public class BudgetShareControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ═══════════════════════════════════════════════
    // Admin — 取得/自動建立分享
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task GetOrCreateShare_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync(
            "/annual-budgets/departments/00000000-0000-0000-0000-000000000001/share");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrCreateShare_ValidDepartment_ReturnsShareInfo()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("分享測試部門A");

        var resp = await AuthorizedGetAsync(ShareUrl(deptId), token);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(resp);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("permission").GetString().Should().Be("Editable");
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreateShare_CalledTwice_ReturnsSameToken()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("分享重複部門");

        var resp1 = await AuthorizedGetAsync(ShareUrl(deptId), token);
        var data1 = (await ReadBodyAsync(resp1)).GetProperty("data");
        var token1 = data1.GetProperty("token").GetString();

        var resp2 = await AuthorizedGetAsync(ShareUrl(deptId), token);
        var data2 = (await ReadBodyAsync(resp2)).GetProperty("data");
        var token2 = data2.GetProperty("token").GetString();

        token1.Should().Be(token2, "同一部門重複取得應回傳相同 Token");
    }

    // ═══════════════════════════════════════════════
    // Admin — 重新產生 Token
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task RegenerateShare_ExistingShare_ReturnsNewToken()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("重新產生部門");

        var resp1 = await AuthorizedGetAsync(ShareUrl(deptId), token);
        var token1 = (await ReadBodyAsync(resp1)).GetProperty("data")
            .GetProperty("token").GetString();

        var resp2 = await AuthorizedPostAsync($"{ShareUrl(deptId)}/regenerate", token, new { });
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var token2 = (await ReadBodyAsync(resp2)).GetProperty("data")
            .GetProperty("token").GetString();

        token1.Should().NotBe(token2, "重新產生應回傳不同 Token");
    }

    // ═══════════════════════════════════════════════
    // Admin — 更新分享
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task UpdateShare_ChangePermission_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("更新權限部門");
        await AuthorizedGetAsync(ShareUrl(deptId), token);

        var resp = await AuthorizedPutAsync(ShareUrl(deptId), token,
            new { permission = "ReadOnly" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadBodyAsync(resp)).GetProperty("data");
        data.GetProperty("permission").GetString().Should().Be("ReadOnly");
    }

    [Fact]
    public async Task UpdateShare_Deactivate_ReturnsInactive()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("停用分享部門");
        await AuthorizedGetAsync(ShareUrl(deptId), token);

        var resp = await AuthorizedPutAsync(ShareUrl(deptId), token,
            new { isActive = false });

        var data = (await ReadBodyAsync(resp)).GetProperty("data");
        data.GetProperty("isActive").GetBoolean().Should().BeFalse();
    }

    // ═══════════════════════════════════════════════
    // Admin — 撤銷分享
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task RevokeShare_ExistingShare_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("撤銷分享部門");
        await AuthorizedGetAsync(ShareUrl(deptId), token);

        var resp = await AuthorizedDeleteAsync(ShareUrl(deptId), token);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeShare_NoShareExists_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("無分享撤銷部門");

        var resp = await AuthorizedDeleteAsync(ShareUrl(deptId), token);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════
    // Public — GET /public/department-budget/{token}
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task PublicGetOverview_ValidToken_ReturnsDepartmentOverview()
    {
        var (shareToken, authToken, _) = await CreateShareSetupAsync("公開總覽部門", 2061);

        var resp = await _client.GetAsync($"/public/department-budget/{shareToken}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(resp);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("departmentName").GetString().Should().Be("公開總覽部門");
        data.GetProperty("permission").GetString().Should().Be("Editable");

        var years = data.GetProperty("availableYears");
        years.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        years[0].GetProperty("year").GetInt32().Should().Be(2061);
    }

    [Fact]
    public async Task PublicGetOverview_InvalidToken_Returns404()
    {
        var resp = await _client.GetAsync("/public/department-budget/nonexistent-token-abc123");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublicGetOverview_InactiveToken_Returns401()
    {
        var (shareToken, authToken, deptId) = await CreateShareSetupAsync("停用公開部門", 2060);

        // 停用 Token
        await AuthorizedPutAsync(ShareUrl(deptId), authToken,
            new { isActive = false });

        var resp = await _client.GetAsync($"/public/department-budget/{shareToken}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════
    // Public — GET /public/department-budget/{token}/years/{year}
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task PublicGetBudget_ValidTokenAndYear_ReturnsBudgetData()
    {
        var (shareToken, _, _) = await CreateShareSetupAsync("公開取得部門", 2059);

        var resp = await _client.GetAsync($"/public/department-budget/{shareToken}/years/2059");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(resp);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("departmentName").GetString().Should().Be("公開取得部門");
        data.GetProperty("year").GetInt32().Should().Be(2059);
        data.GetProperty("effectivePermission").GetString().Should().Be("Editable");
        data.GetProperty("budgetStatus").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task PublicGetBudget_NonExistentYear_Returns404()
    {
        var (shareToken, _, _) = await CreateShareSetupAsync("年度不存在部門", 2058);

        var resp = await _client.GetAsync($"/public/department-budget/{shareToken}/years/9999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════════
    // Public — PUT /public/department-budget/{token}/years/{year}/items
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task PublicSaveItems_EditableToken_SavesSuccessfully()
    {
        var (shareToken, _, _) = await CreateShareSetupAsync("公開儲存部門", 2057);

        var resp = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2057/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "活動A", contentItem = "項目A", amount = 10000m, note = "備註A" },
                    new { sequence = 2, activityName = "活動B", contentItem = "項目B", amount = 20000m, note = (string?)null },
                },
            });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(resp);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var items = body.GetProperty("data");
        items.GetArrayLength().Should().Be(2);
        items[0].GetProperty("activityName").GetString().Should().Be("活動A");
        items[1].GetProperty("amount").GetDecimal().Should().Be(20000m);
    }

    [Fact]
    public async Task PublicSaveItems_ReadOnlyToken_Returns401()
    {
        var (shareToken, authToken, deptId) = await CreateShareSetupAsync("唯讀公開部門", 2056);

        // 設定為唯讀
        await AuthorizedPutAsync(ShareUrl(deptId), authToken,
            new { permission = "ReadOnly" });

        var resp = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2056/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "活動X", contentItem = "項目X", amount = 5000m },
                },
            });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicSaveItems_StatusLocked_Returns401()
    {
        var (shareToken, authToken, _) = await CreateShareSetupAsync("狀態鎖定部門", 2055);

        // 將狀態從 Draft 改為 Submitted
        await AuthorizedPutAsync($"/annual-budgets/2055/status", authToken,
            new { status = "Submitted" });

        var resp = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2055/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "活動Y", contentItem = "項目Y", amount = 5000m },
                },
            });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicSaveItems_InvalidToken_Returns404()
    {
        var resp = await _client.PutAsJsonAsync(
            "/public/department-budget/bad-token-xyz/years/2054/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "活動Z", contentItem = "項目Z", amount = 1000m },
                },
            });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublicSaveItems_InactiveToken_Returns401()
    {
        var (shareToken, authToken, deptId) = await CreateShareSetupAsync("停用寫入部門", 2053);

        await AuthorizedPutAsync(ShareUrl(deptId), authToken,
            new { isActive = false });

        var resp = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2053/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "活動W", contentItem = "項目W", amount = 1000m },
                },
            });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════
    // 狀態連動：EffectivePermission 自動降級
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task PublicGetBudget_StatusSubmitted_EffectivePermissionReadOnly()
    {
        var (shareToken, authToken, _) = await CreateShareSetupAsync("狀態連動部門", 2052);

        // 狀態改為 Submitted
        await AuthorizedPutAsync($"/annual-budgets/2052/status", authToken,
            new { status = "Submitted" });

        var resp = await _client.GetAsync(
            $"/public/department-budget/{shareToken}/years/2052");
        var data = (await ReadBodyAsync(resp)).GetProperty("data");

        data.GetProperty("effectivePermission").GetString().Should().Be("ReadOnly");
        data.GetProperty("budgetStatus").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task PublicGetBudget_StatusBackToDraft_EffectivePermissionRestored()
    {
        var (shareToken, authToken, _) = await CreateShareSetupAsync("狀態復原部門", 2051);

        // 狀態改為 Submitted，再改回 Draft
        await AuthorizedPutAsync($"/annual-budgets/2051/status", authToken,
            new { status = "Submitted" });
        await AuthorizedPutAsync($"/annual-budgets/2051/status", authToken,
            new { status = "Draft" });

        var resp = await _client.GetAsync(
            $"/public/department-budget/{shareToken}/years/2051");
        var data = (await ReadBodyAsync(resp)).GetProperty("data");

        data.GetProperty("effectivePermission").GetString().Should().Be("Editable");
    }

    // ═══════════════════════════════════════════════
    // Public — 新增後更新項目（upsert）
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task PublicSaveItems_UpdateExistingItem_ReturnsUpdated()
    {
        var (shareToken, _, _) = await CreateShareSetupAsync("更新項目部門", 2050);

        // 先新增一筆
        var resp1 = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2050/items", new
            {
                items = new[]
                {
                    new { sequence = 1, activityName = "原始活動", contentItem = "原始項目", amount = 5000m },
                },
            });
        var items1 = (await ReadBodyAsync(resp1)).GetProperty("data");
        var itemId = items1[0].GetProperty("id").GetString();

        // 更新既有項目
        var resp2 = await _client.PutAsJsonAsync(
            $"/public/department-budget/{shareToken}/years/2050/items", new
            {
                items = new[]
                {
                    new { id = itemId, sequence = 1, activityName = "更新活動", contentItem = "更新項目", amount = 8000m },
                },
            });

        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var items2 = (await ReadBodyAsync(resp2)).GetProperty("data");
        items2.GetArrayLength().Should().Be(1);
        items2[0].GetProperty("activityName").GetString().Should().Be("更新活動");
        items2[0].GetProperty("amount").GetDecimal().Should().Be(8000m);
    }

    // ═══════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════

    /// <summary>建立完整分享設定（建立部門、初始化預算、取得自動建立的 Token）</summary>
    private async Task<(string ShareToken, string AuthToken, Guid DepartmentId)> CreateShareSetupAsync(
        string departmentName, int year)
    {
        var authToken = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync(departmentName);
        await InitializeAnnualBudgetAsync(year, authToken);

        // GetOrCreate 會自動建立 Token
        var resp = await AuthorizedGetAsync(ShareUrl(deptId), authToken);
        var data = (await ReadBodyAsync(resp)).GetProperty("data");
        var shareToken = data.GetProperty("token").GetString()!;

        return (shareToken, authToken, deptId);
    }

    private static string ShareUrl(Guid departmentId)
        => $"/annual-budgets/departments/{departmentId}/share";

    private async Task<Guid> EnsureDepartmentAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = db.SportsDepartments.FirstOrDefault(d => d.Name == name);
        if (existing is not null)
            return existing.Id;

        var entity = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.SportsDepartments.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private Task<HttpResponseMessage> AuthorizedGetAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Get, url, token));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private Task<HttpResponseMessage> InitializeAnnualBudgetAsync(int year, string token)
        => AuthorizedGetAsync($"/annual-budgets/{year}", token);

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
