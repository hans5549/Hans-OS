using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class TsfSettingsControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Departments ─────────────────────────────────

    [Fact]
    public async Task GetDepartments_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/tsf-settings/departments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepartments_Authorized_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var request = AuthGet("/tsf-settings/departments", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task CreateDepartment_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/tsf-settings/departments", new
        {
            name = "測試部門",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDepartment_Valid_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync();
        var request = AuthPost("/tsf-settings/departments", token, new
        {
            name = "籃球部",
            note = "籃球相關活動",
        });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("籃球部");
        data.GetProperty("note").GetString().Should().Be("籃球相關活動");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateDepartment_EmptyName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var request = AuthPost("/tsf-settings/departments", token, new
        {
            name = "",
        });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        // Create first
        var req1 = AuthPost("/tsf-settings/departments", token, new { name = "重複部門" });
        await _client.SendAsync(req1);

        // Create duplicate
        var req2 = AuthPost("/tsf-settings/departments", token, new { name = "重複部門" });
        var response = await _client.SendAsync(req2);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDepartment_Valid_Returns200()
    {
        var token = await LoginAndGetTokenAsync();

        // Create
        var createReq = AuthPost("/tsf-settings/departments", token, new { name = "待更新部門" });
        var createResp = await _client.SendAsync(createReq);
        var createBody = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Update
        var updateReq = AuthPut($"/tsf-settings/departments/{id}", token, new
        {
            name = "已更新部門",
            note = "備註",
        });
        var response = await _client.SendAsync(updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task UpdateDepartment_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var id = Guid.NewGuid();
        var request = AuthPut($"/tsf-settings/departments/{id}", token, new { name = "不存在" });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDepartment_Valid_Returns200()
    {
        var token = await LoginAndGetTokenAsync();

        // Create
        var createReq = AuthPost("/tsf-settings/departments", token, new { name = "待刪除部門" });
        var createResp = await _client.SendAsync(createReq);
        var createBody = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Delete
        var deleteReq = AuthDelete($"/tsf-settings/departments/{id}", token);
        var response = await _client.SendAsync(deleteReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteDepartment_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var id = Guid.NewGuid();
        var request = AuthDelete($"/tsf-settings/departments/{id}", token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDepartment_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/tsf-settings/departments/{Guid.NewGuid()}", new
        {
            name = "test",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDepartment_Unauthorized_Returns401()
    {
        var response = await _client.DeleteAsync($"/tsf-settings/departments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Bank Balances ───────────────────────────────

    [Fact]
    public async Task GetBankBalances_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/tsf-settings/bank-balances");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBankBalances_Authorized_ReturnsList()
    {
        var token = await LoginAndGetTokenAsync();
        var request = AuthGet("/tsf-settings/bank-balances", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task UpdateBankBalance_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var id = Guid.NewGuid();
        var request = AuthPut($"/tsf-settings/bank-balances/{id}", token, new
        {
            initialAmount = 1000m,
        });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBankBalance_Unauthorized_Returns401()
    {
        var id = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/tsf-settings/bank-balances/{id}", new
        {
            initialAmount = 1000m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateBankBalance_ValidData_Returns200()
    {
        var token = await LoginAndGetTokenAsync();

        // 直接透過 DI 容器建立 BankInitialBalance（避免依賴 Import 端點）
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.BankInitialBalances.Add(new BankInitialBalance
            {
                Id = Guid.NewGuid(),
                BankName = "測試銀行",
                InitialAmount = 10000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // 取得現有的 bank balances
        var listResp = await _client.SendAsync(AuthGet("/tsf-settings/bank-balances", token));
        var listBody = await listResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var balances = listBody.GetProperty("data");
        balances.GetArrayLength().Should().BeGreaterThan(0);

        var balanceId = balances[0].GetProperty("id").GetString();

        // 更新金額
        var updateReq = AuthPut($"/tsf-settings/bank-balances/{balanceId}", token, new
        {
            initialAmount = 99999.99m,
        });
        var response = await _client.SendAsync(updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
    }

    // ── Helpers ──────────────────────────────────────

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

    private static HttpRequestMessage AuthGet(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthPost(string url, string token, object data)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthPut(string url, string token, object data)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(data),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthDelete(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
