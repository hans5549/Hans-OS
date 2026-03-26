using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class AnnualBudgetControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetOverview ─────────────────────────────────

    [Fact]
    public async Task GetOverview_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/annual-budgets/2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_ValidYear_AutoInitializesAndReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/annual-budgets/2099", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("year").GetInt32().Should().Be(2099);
        data.GetProperty("status").GetString().Should().Be("Draft");
        data.GetProperty("departments").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetOverview_SameYear_ReturnsSameBudget()
    {
        var token = await LoginAndGetTokenAsync();

        var resp1 = await AuthorizedGetAsync("/annual-budgets/2098", token);
        var body1 = await ReadBodyAsync(resp1);
        var id1 = body1.GetProperty("data").GetProperty("id").GetString();

        var resp2 = await AuthorizedGetAsync("/annual-budgets/2098", token);
        var body2 = await ReadBodyAsync(resp2);
        var id2 = body2.GetProperty("data").GetProperty("id").GetString();

        id1.Should().Be(id2);
    }

    [Fact]
    public async Task GetOverview_WithDepartments_ShowsDepartmentSummaries()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算測試部門A");
        var response = await AuthorizedGetAsync("/annual-budgets/2097", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");
        departments.GetArrayLength().Should().BeGreaterThan(0);

        var found = false;
        foreach (var dept in departments.EnumerateArray())
        {
            if (dept.GetProperty("departmentId").GetString() == deptId.ToString())
            {
                found = true;
                dept.GetProperty("budgetAmount").GetDecimal().Should().Be(0);
                dept.GetProperty("itemCount").GetInt32().Should().Be(0);
            }
        }

        found.Should().BeTrue("部門應出現在年度預算中");
    }

    // ── UpdateStatus ────────────────────────────────

    [Fact]
    public async Task UpdateStatus_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/annual-budgets/2026/status", new
        {
            status = "Submitted",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2096, token);
        var response = await AuthorizedPutAsync("/annual-budgets/2096/status", token, new { status = "Submitted" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp2 = await AuthorizedGetAsync("/annual-budgets/2096", token);
        var body = await ReadBodyAsync(resp2);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2095, token);

        var response = await AuthorizedPutAsync("/annual-budgets/2095/status", token, new { status = "InvalidStatus" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentYear_Returns404()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPutAsync("/annual-budgets/2050/status", token, new { status = "Draft" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2085, token);

        // Draft → Approved 不允許（必須先 Submitted）
        var response = await AuthorizedPutAsync("/annual-budgets/2085/status", token, new { status = "Approved" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOverview_YearOutOfRange_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/annual-budgets/1999", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetDepartmentItems ──────────────────────────

    [Fact]
    public async Task GetDepartmentItems_Unauthorized_Returns401()
    {
        var deptId = Guid.NewGuid();
        var response = await _client.GetAsync($"/annual-budgets/2026/departments/{deptId}/items");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepartmentItems_EmptyDepartment_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算空部門");
        await InitializeAnnualBudgetAsync(2094, token);
        var response = await AuthorizedGetAsync(DepartmentItemsUrl(2094, deptId), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ── SaveDepartmentItems ─────────────────────────

    [Fact]
    public async Task SaveDepartmentItems_Unauthorized_Returns401()
    {
        var deptId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync(
            $"/annual-budgets/2026/departments/{deptId}/items",
            new { items = Array.Empty<object>() });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveDepartmentItems_ValidItems_ReturnsCreatedItems()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算儲存部門");
        await InitializeAnnualBudgetAsync(2093, token);

        var response = await SaveDepartmentItemsAsync(
            2093,
            deptId,
            token,
            BudgetItemPayload(1, "全國排球賽", "場地費", 50000m, note: "中央大學體育館"),
            BudgetItemPayload(2, "全國排球賽", "裁判費", 14000m, note: "20人×700元"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
        data[0].GetProperty("activityName").GetString().Should().Be("全國排球賽");
        data[0].GetProperty("contentItem").GetString().Should().Be("場地費");
        data[0].GetProperty("amount").GetDecimal().Should().Be(50000m);
        data[1].GetProperty("contentItem").GetString().Should().Be("裁判費");
    }

    [Fact]
    public async Task SaveDepartmentItems_UpdateExisting_ModifiesItems()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算更新部門");
        await InitializeAnnualBudgetAsync(2092, token);

        var createResp = await SaveDepartmentItemsAsync(
            2092,
            deptId,
            token,
            BudgetItemPayload(1, "原始活動", "原始項目", 1000m));
        var createBody = await ReadBodyAsync(createResp);
        var itemId = createBody.GetProperty("data")[0].GetProperty("id").GetString();

        var updateResp = await SaveDepartmentItemsAsync(
            2092,
            deptId,
            token,
            BudgetItemPayload(
                1,
                "已更新活動",
                "已更新項目",
                2000m,
                id: itemId,
                actualAmount: 1800m,
                actualNote: "實際支出"));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        var updated = updateBody.GetProperty("data")[0];
        updated.GetProperty("activityName").GetString().Should().Be("已更新活動");
        updated.GetProperty("amount").GetDecimal().Should().Be(2000m);
        updated.GetProperty("actualAmount").GetDecimal().Should().Be(1800m);
    }

    [Fact]
    public async Task SaveDepartmentItems_RemoveItems_DeletesOmitted()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算刪除部門");
        await InitializeAnnualBudgetAsync(2091, token);
        await SaveDepartmentItemsAsync(
            2091,
            deptId,
            token,
            BudgetItemPayload(1, "A", "A1", 100m),
            BudgetItemPayload(2, "B", "B1", 200m));

        var resp = await SaveDepartmentItemsAsync(
            2091,
            deptId,
            token,
            BudgetItemPayload(1, "A", "A1", 100m));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(resp);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SaveDepartmentItems_EmptyList_ClearsAll()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算清空部門");
        await InitializeAnnualBudgetAsync(2090, token);
        await SaveDepartmentItemsAsync(
            2090,
            deptId,
            token,
            BudgetItemPayload(1, "X", "X1", 500m));

        var resp = await SaveDepartmentItemsAsync(2090, deptId, token);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(resp);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetOverview_AfterSaveItems_ReflectsTotals()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算合計部門");
        await InitializeAnnualBudgetAsync(2089, token);
        await SaveDepartmentItemsAsync(
            2089,
            deptId,
            token,
            BudgetItemPayload(1, "E1", "C1", 10000m),
            BudgetItemPayload(2, "E2", "C2", 20000m));

        var resp = await AuthorizedGetAsync("/annual-budgets/2089", token);
        var body = await ReadBodyAsync(resp);
        var data = body.GetProperty("data");

        data.GetProperty("totalBudget").GetDecimal().Should().BeGreaterThanOrEqualTo(30000m);
    }

    // ── Helpers ──────────────────────────────────────

    private async Task<Guid> EnsureDepartmentAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = db.SportsDepartments.FirstOrDefault(d => d.Name == name);
        if (existing is not null)
        {
            return existing.Id;
        }

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
        => _client.SendAsync(AuthGet(url, token));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(AuthPut(url, token, data));

    private Task<HttpResponseMessage> InitializeAnnualBudgetAsync(int year, string token)
        => AuthorizedGetAsync($"/annual-budgets/{year}", token);

    private Task<HttpResponseMessage> SaveDepartmentItemsAsync(int year, Guid departmentId, string token, params object[] items)
        => AuthorizedPutAsync(DepartmentItemsUrl(year, departmentId), token, new { items });

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

    private static string DepartmentItemsUrl(int year, Guid departmentId)
        => $"/annual-budgets/{year}/departments/{departmentId}/items";

    private static object BudgetItemPayload(
        int sequence,
        string activityName,
        string contentItem,
        decimal amount,
        string? note = null,
        decimal? actualAmount = null,
        string? actualNote = null,
        string? id = null)
        => new
        {
            id,
            sequence,
            activityName,
            contentItem,
            amount,
            note,
            actualAmount,
            actualNote,
        };

    private static HttpRequestMessage AuthGet(string url, string token)
        => CreateAuthorizedRequest(HttpMethod.Get, url, token);

    private static HttpRequestMessage AuthPut(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data));

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
