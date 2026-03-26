using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class BankTransactionControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ── Authentication Helpers ──────────────────────────────

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
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

    // ── GET Transactions ────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_EmptyMonth_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetTransactions_FullYear_ReturnsAllTransactions()
    {
        var token = await LoginAndGetTokenAsync();

        // Create two transactions in different months
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-01-15",
            description = "一月收入",
            amount = 1000,
        }));
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2026-06-15",
            description = "六月支出",
            amount = 500,
        }));

        // Get full year (no month parameter)
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    // ── GET Summary ─────────────────────────────────────────

    [Fact]
    public async Task GetSummary_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行/summary?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_EmptyMonth_ReturnsZeroSummary()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行/summary?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("totalIncome").GetDecimal().Should().Be(0);
        data.GetProperty("totalExpense").GetDecimal().Should().Be(0);
    }

    // ── POST Create ─────────────────────────────────────────

    [Fact]
    public async Task CreateTransaction_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/bank-transactions/上海銀行", new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTransaction_ValidIncome_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000.50,
            fee = 15,
            hasReceipt = true,
            receiptMailed = false,
            requestingUnit = "教務處",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("bankName").GetString().Should().Be("上海銀行");
        data.GetProperty("description").GetString().Should().Be("會費收入");
        data.GetProperty("amount").GetDecimal().Should().Be(5000.50m);
        data.GetProperty("fee").GetDecimal().Should().Be(15m);
        data.GetProperty("hasReceipt").GetBoolean().Should().BeTrue();
        data.GetProperty("receiptMailed").GetBoolean().Should().BeFalse();
        data.GetProperty("requestingUnit").GetString().Should().Be("教務處");
    }

    [Fact]
    public async Task CreateTransaction_InvalidDepartment_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000,
            departmentId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_MissingDescription_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "",
            amount = 5000,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT Update ──────────────────────────────────────────

    [Fact]
    public async Task UpdateTransaction_ValidData_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync();

        // Create first
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/合作金庫", token, new
        {
            transactionType = 1,
            transactionDate = "2026-04-01",
            description = "場地租金",
            amount = 2000,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Update
        var updateResponse = await _client.SendAsync(AuthPut($"/bank-transactions/{id}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-04-02",
            description = "場地租金（更新）",
            amount = 2500,
            requestingUnit = "總務處",
        }));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/合作金庫?year=2026&month=4", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = getBody.GetProperty("data");
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var updated = transactions[0];
        updated.GetProperty("description").GetString().Should().Be("場地租金（更新）");
        updated.GetProperty("amount").GetDecimal().Should().Be(2500m);
        updated.GetProperty("requestingUnit").GetString().Should().Be("總務處");
    }

    [Fact]
    public async Task UpdateTransaction_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPut($"/bank-transactions/{Guid.NewGuid()}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "不存在",
            amount = 100,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_ValidId_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync();

        // Create
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-05-01",
            description = "待刪除",
            amount = 100,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Delete
        var deleteResponse = await _client.SendAsync(AuthDelete($"/bank-transactions/{id}", token));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify gone
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026&month=5", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getBody.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task DeleteTransaction_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthDelete($"/bank-transactions/{Guid.NewGuid()}", token));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Running Balance Correctness ─────────────────────────

    [Fact]
    public async Task RunningBalance_CalculatesCorrectlyWithFees()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "合作金庫";

        // Create transactions in order
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-07-01",
            description = "收入1",
            amount = 10000,
            fee = 0,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-07-05",
            description = "支出1",
            amount = 3000,
            fee = 15,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-07-10",
            description = "收入2",
            amount = 5000,
            fee = 10,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}?year=2026&month=7", token));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(3);

        // Opening balance is 0 (no initial balance seeded for in-memory test)
        // After income 10000, fee 0: balance = 0 + 10000 - 0 = 10000
        data[0].GetProperty("runningBalance").GetDecimal().Should().Be(10000m);
        // After expense 3000, fee 15: balance = 10000 - 3000 - 15 = 6985
        data[1].GetProperty("runningBalance").GetDecimal().Should().Be(6985m);
        // After income 5000, fee 10: balance = 6985 + 5000 - 10 = 11975
        data[2].GetProperty("runningBalance").GetDecimal().Should().Be(11975m);
    }

    [Fact]
    public async Task Summary_CalculatesCorrectly()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "上海銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-08-01",
            description = "收入A",
            amount = 20000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-08-10",
            description = "收入B",
            amount = 5000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-08-15",
            description = "支出A",
            amount = 8000,
            fee = 30,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/summary?year=2026&month=8", token));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");

        data.GetProperty("totalIncome").GetDecimal().Should().Be(25000m);
        // totalExpense = expense amounts + all fees = 8000 + 30 = 8030
        data.GetProperty("totalExpense").GetDecimal().Should().Be(8030m);
    }

    // ── GET Summary (Happy Path) ───────────────────────────────

    [Fact]
    public async Task GetSummary_WithData_ReturnsCorrectSummary()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "合作金庫";

        // 建立收入與支出（使用不同銀行避免其他測試資料干擾）
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-09-01",
            description = "會費收入",
            amount = 15000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-09-15",
            description = "場地費",
            amount = 3000,
            fee = 20,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/summary?year=2026&month=9", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("totalIncome").GetDecimal().Should().Be(15000m);
        data.GetProperty("totalExpense").GetDecimal().Should().Be(3020m);
    }

    // ── PUT Update (Unauthorized) ───────────────────────────

    [Fact]
    public async Task UpdateTransaction_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/bank-transactions/{Guid.NewGuid()}", new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "test",
            amount = 100,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DELETE (Unauthorized) ────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_Unauthorized_Returns401()
    {
        var response = await _client.DeleteAsync($"/bank-transactions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Import ────────────────────────────────────────────────

    [Fact]
    public async Task ImportHistoricalData_Unauthorized_Returns401()
    {
        var response = await _client.PostAsync("/bank-transactions/import", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Export ───────────────────────────────────────────────

    [Fact]
    public async Task ExportExcel_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行/export?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportExcel_ReturnsXlsx()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行/export?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
