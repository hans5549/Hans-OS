using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class FinanceAccountControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetAccounts ─────────────────────────────────

    [Fact]
    public async Task GetAccounts_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/finance/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccounts_Empty_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/finance/accounts", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ── CreateAccount ───────────────────────────────

    [Fact]
    public async Task CreateAccount_ValidCash_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name = "現金錢包",
            accountType = "Cash",
            initialBalance = 1000m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("現金錢包");
        data.GetProperty("accountType").GetString().Should().Be("Cash");
        data.GetProperty("initialBalance").GetDecimal().Should().Be(1000m);
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAccount_AllTypes_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var types = new[] { "Cash", "Bank", "CreditCard", "EPayment", "Investment" };

        foreach (var accountType in types)
        {
            var response = await AuthorizedPostAsync("/finance/accounts", token, new
            {
                name = $"測試帳戶_{accountType}",
                accountType,
                initialBalance = 0m,
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var getResponse = await AuthorizedGetAsync("/finance/accounts", token);
        var body = await ReadBodyAsync(getResponse);
        var data = body.GetProperty("data");

        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(types.Length);

        var returnedTypes = new HashSet<string>();
        foreach (var account in data.EnumerateArray())
        {
            var t = account.GetProperty("accountType").GetString()!;
            if (t.StartsWith("Cash") || t.StartsWith("Bank") || t.StartsWith("CreditCard")
                || t.StartsWith("EPayment") || t.StartsWith("Investment"))
            {
                returnedTypes.Add(t);
            }
        }

        returnedTypes.Should().BeEquivalentTo(types);
    }

    [Fact]
    public async Task CreateAccount_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new
        {
            name = "重複帳戶測試",
            accountType = "Cash",
            initialBalance = 0m,
        };

        var first = await AuthorizedPostAsync("/finance/accounts", token, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await AuthorizedPostAsync("/finance/accounts", token, payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAccount_EmptyName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name = "",
            accountType = "Cash",
            initialBalance = 0m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── UpdateAccount ───────────────────────────────

    [Fact]
    public async Task UpdateAccount_ValidData_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name = "更新前帳戶",
            accountType = "Bank",
            initialBalance = 500m,
        });
        var createBody = await ReadBodyAsync(createResp);
        var accountId = createBody.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/finance/accounts/{accountId}", token, new
        {
            name = "更新後帳戶",
            accountType = "Bank",
            initialBalance = 500m,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("更新後帳戶");
    }

    // ── DeleteAccount ───────────────────────────────

    [Fact]
    public async Task DeleteAccount_NoTransactions_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name = "待刪除帳戶",
            accountType = "Cash",
            initialBalance = 0m,
        });
        var createBody = await ReadBodyAsync(createResp);
        var accountId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResp = await AuthorizedDeleteAsync($"/finance/accounts/{accountId}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await AuthorizedGetAsync("/finance/accounts", token);
        var getBody = await ReadBodyAsync(getResp);
        var accounts = getBody.GetProperty("data");

        var found = false;
        foreach (var account in accounts.EnumerateArray())
        {
            if (account.GetProperty("id").GetString() == accountId)
            {
                found = true;
            }
        }

        found.Should().BeFalse("帳戶應已被刪除");
    }

    // ── GetBalances ─────────────────────────────────

    [Fact]
    public async Task GetBalances_WithTransactions_CalculatesCorrectly()
    {
        var token = await LoginAndGetTokenAsync();

        // Create account with initial balance 5000
        var createResp = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name = "餘額測試帳戶",
            accountType = "Cash",
            initialBalance = 5000m,
        });
        var createBody = await ReadBodyAsync(createResp);
        var accountId = Guid.Parse(createBody.GetProperty("data").GetProperty("id").GetString()!);

        // Get userId by calling user/info
        var userResp = await AuthorizedGetAsync("/user/info", token);
        var userBody = await ReadBodyAsync(userResp);
        var userId = userBody.GetProperty("data").GetProperty("userId").GetString()!;

        // Insert an expense transaction directly via DB
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;
        db.FinanceTransactions.Add(new FinanceTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TransactionType = FinanceTransactionType.Expense,
            Amount = 1000m,
            TransactionDate = DateOnly.FromDateTime(now),
            AccountId = accountId,
            Note = "測試支出",
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();

        // GET /finance/accounts/balances should show currentBalance = 4000
        var balanceResp = await AuthorizedGetAsync("/finance/accounts/balances", token);
        balanceResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceBody = await ReadBodyAsync(balanceResp);
        var balances = balanceBody.GetProperty("data");

        var found = false;
        foreach (var b in balances.EnumerateArray())
        {
            if (b.GetProperty("id").GetString() == accountId.ToString())
            {
                found = true;
                b.GetProperty("initialBalance").GetDecimal().Should().Be(5000m);
                b.GetProperty("currentBalance").GetDecimal().Should().Be(4000m);
            }
        }

        found.Should().BeTrue("帳戶應出現在餘額清單中");
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

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

    private Task<HttpResponseMessage> AuthorizedGetAsync(string url, string token)
        => _client.SendAsync(AuthGet(url, token));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(AuthPost(url, token, data));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(AuthPut(url, token, data));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private static HttpRequestMessage AuthGet(string url, string token)
        => CreateAuthorizedRequest(HttpMethod.Get, url, token);

    private static HttpRequestMessage AuthPost(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data));

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
