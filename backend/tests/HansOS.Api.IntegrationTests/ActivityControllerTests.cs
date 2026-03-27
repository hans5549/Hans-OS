using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class ActivityControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetList ──────────────────────────────────────

    [Fact]
    public async Task GetList_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/activities?year=2026&month=1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetList_NoActivities_ReturnsEmptyArray()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/activities?year=2090&month=1", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetList_WithActivities_ReturnsFilteredList()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("活動列表測試部門");

        await CreateActivityAsync(token, deptId, 2091, 3, "三月活動A");
        await CreateActivityAsync(token, deptId, 2091, 3, "三月活動B");
        await CreateActivityAsync(token, deptId, 2091, 4, "四月活動");

        var response = await AuthorizedGetAsync("/activities?year=2091&month=3", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    // ── GetMonthSummaries ────────────────────────────

    [Fact]
    public async Task GetMonthSummaries_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/activities/month-summaries?year=2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMonthSummaries_WithData_ReturnsMonthlySummaries()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("月統計測試部門");

        await CreateActivityAsync(token, deptId, 2092, 5, "五月活動", 1000m);
        await CreateActivityAsync(token, deptId, 2092, 5, "五月活動2", 2000m);
        await CreateActivityAsync(token, deptId, 2092, 6, "六月活動", 500m);

        var response = await AuthorizedGetAsync("/activities/month-summaries?year=2092", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    // ── GetDetail ────────────────────────────────────

    [Fact]
    public async Task GetDetail_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync($"/activities/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetail_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync($"/activities/{Guid.NewGuid()}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDetail_ValidId_ReturnsActivityWithExpenses()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("明細測試部門");

        var createResp = await CreateActivityAsync(token, deptId, 2093, 7, "明細活動", 1500m);
        var createBody = await ReadBodyAsync(createResp);
        var activityId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        var response = await AuthorizedGetAsync($"/activities/{activityId}", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("明細活動");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(1500m);
    }

    // ── Create ───────────────────────────────────────

    [Fact]
    public async Task Create_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/activities", new
        {
            departmentId = Guid.NewGuid(),
            year = 2026,
            month = 1,
            name = "test",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_SimpleActivity_ReturnsCreatedActivity()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("新增測試部門");

        var response = await CreateActivityAsync(token, deptId, 2094, 1, "簡單活動", 500m);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("簡單活動");
        data.GetProperty("ungroupedExpenses").GetArrayLength().Should().Be(1);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(500m);
    }

    [Fact]
    public async Task Create_ActivityWithGroups_ReturnsGroupedExpenses()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("分組測試部門");

        var payload = new
        {
            departmentId = deptId,
            year = 2094,
            month = 2,
            name = "暑期籃球營",
            description = "複雜活動測試",
            groups = new[]
            {
                new
                {
                    name = "第一梯次",
                    sequence = 1,
                    expenses = new[]
                    {
                        new { description = "場地費", amount = 3000m, sequence = 1 },
                        new { description = "裝備費", amount = 5000m, sequence = 2 },
                    }
                },
                new
                {
                    name = "第二梯次",
                    sequence = 2,
                    expenses = new[]
                    {
                        new { description = "場地費", amount = 3000m, sequence = 1 },
                        new { description = "餐費", amount = 2000m, sequence = 2 },
                    }
                },
            },
        };

        var response = await AuthorizedPostAsync("/activities", token, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("暑期籃球營");
        data.GetProperty("groups").GetArrayLength().Should().Be(2);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(13000m);
    }

    [Fact]
    public async Task Create_InvalidDepartment_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var payload = new
        {
            departmentId = Guid.NewGuid(),
            year = 2094,
            month = 3,
            name = "無效部門活動",
            expenses = new[]
            {
                new { description = "交通費", amount = 100m, sequence = 1 },
            },
        };

        var response = await AuthorizedPostAsync("/activities", token, payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_MissingName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("驗證測試部門");

        var payload = new
        {
            departmentId = deptId,
            year = 2094,
            month = 4,
            name = "",
        };

        var response = await AuthorizedPostAsync("/activities", token, payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Update ───────────────────────────────────────

    [Fact]
    public async Task Update_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/activities/{Guid.NewGuid()}", new
        {
            name = "updated",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ValidActivity_ReturnsUpdatedData()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("更新測試部門");

        var createResp = await CreateActivityAsync(token, deptId, 2095, 1, "原始名稱", 100m);
        var createBody = await ReadBodyAsync(createResp);
        var activityId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        var updatePayload = new
        {
            name = "更新後名稱",
            description = "新增說明",
            expenses = new[]
            {
                new { description = "更新的開銷", amount = 200m, sequence = 1 },
                new { description = "新的開銷", amount = 300m, sequence = 2 },
            },
        };

        var response = await AuthorizedPutAsync($"/activities/{activityId}", token, updatePayload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("更新後名稱");
        data.GetProperty("description").GetString().Should().Be("新增說明");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(500m);
        data.GetProperty("ungroupedExpenses").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new { name = "不存在的活動" };

        var response = await AuthorizedPutAsync($"/activities/{Guid.NewGuid()}", token, payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Delete ───────────────────────────────────────

    [Fact]
    public async Task Delete_Unauthorized_Returns401()
    {
        var response = await _client.DeleteAsync($"/activities/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ValidActivity_Returns200AndRemovesData()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("刪除測試部門");

        var createResp = await CreateActivityAsync(token, deptId, 2096, 1, "待刪除活動", 100m);
        var createBody = await ReadBodyAsync(createResp);
        var activityId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        var deleteResponse = await AuthorizedDeleteAsync($"/activities/{activityId}", token);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await AuthorizedGetAsync($"/activities/{activityId}", token);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedDeleteAsync($"/activities/{Guid.NewGuid()}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────

    private async Task<HttpResponseMessage> CreateActivityAsync(
        string token, Guid departmentId, int year, int month,
        string name, decimal expenseAmount = 0m)
    {
        var payload = new
        {
            departmentId,
            year,
            month,
            name,
            expenses = expenseAmount > 0
                ? new[] { new { description = "開銷", amount = expenseAmount, sequence = 1 } }
                : Array.Empty<object>(),
        };
        return await AuthorizedPostAsync("/activities", token, payload);
    }

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

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

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
