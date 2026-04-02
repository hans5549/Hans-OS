using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class FinanceTransactionControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GET /finance/transactions ───────────────────

    [Fact]
    public async Task GetTransactions_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/finance/transactions?year=2025&month=4");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /finance/transactions (Expense) ────────

    [Fact]
    public async Task CreateExpense_ValidData_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);
        var categoryId = await CreateCategoryAndGetIdAsync(token, "餐飲", "Expense");

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense",
            amount = 500,
            transactionDate = "2025-04-01",
            categoryId,
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("transactionType").GetString().Should().Be("Expense");
        data.GetProperty("amount").GetDecimal().Should().Be(500);
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    // ── POST /finance/transactions (Income) ─────────

    [Fact]
    public async Task CreateIncome_ValidData_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);
        var categoryId = await CreateCategoryAndGetIdAsync(token, "薪水", "Income");

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Income",
            amount = 50000,
            transactionDate = "2025-04-01",
            categoryId,
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("transactionType").GetString().Should().Be("Income");
        data.GetProperty("amount").GetDecimal().Should().Be(50000);
    }

    // ── POST /finance/transactions (Transfer) ───────

    [Fact]
    public async Task CreateTransfer_ValidData_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var fromAccountId = await CreateAccountAndGetIdAsync(token, "來源帳戶");
        var toAccountId = await CreateAccountAndGetIdAsync(token, "目標帳戶");

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Transfer",
            amount = 1000,
            transactionDate = "2025-04-01",
            accountId = fromAccountId,
            toAccountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("transactionType").GetString().Should().Be("Transfer");
    }

    [Fact]
    public async Task CreateTransfer_MissingToAccount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Transfer",
            amount = 1000,
            transactionDate = "2025-04-01",
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Validation ──────────────────────────────────

    [Fact]
    public async Task CreateExpense_MissingCategory_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense",
            amount = 500,
            transactionDate = "2025-04-01",
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateExpense_ZeroAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);
        var categoryId = await CreateCategoryAndGetIdAsync(token, "零元分類", "Expense");

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense",
            amount = 0,
            transactionDate = "2025-04-01",
            categoryId,
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateExpense_NegativeAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token);
        var categoryId = await CreateCategoryAndGetIdAsync(token, "負數分類", "Expense");

        var response = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense",
            amount = -100,
            transactionDate = "2025-04-01",
            categoryId,
            accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /finance/transactions (month filter) ────

    [Fact]
    public async Task GetTransactions_MonthFilter_ReturnsDailyGroups()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token, "月篩選帳戶");
        var categoryId = await CreateCategoryAndGetIdAsync(token, "月篩選分類", "Expense");

        // Create 3 transactions on different dates in the same month
        await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 100,
            transactionDate = "2025-04-05", categoryId, accountId,
        });
        await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 200,
            transactionDate = "2025-04-10", categoryId, accountId,
        });
        await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 300,
            transactionDate = "2025-04-15", categoryId, accountId,
        });

        var response = await AuthorizedGetAsync("/finance/transactions?year=2025&month=4", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(3);

        // Each group should have date and transactions
        var firstGroup = data[0];
        firstGroup.TryGetProperty("date", out _).Should().BeTrue();
        firstGroup.TryGetProperty("transactions", out _).Should().BeTrue();
    }

    // ── GET /finance/transactions/summary ───────────

    [Fact]
    public async Task GetMonthlySummary_CalculatesCorrectly()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token, "摘要帳戶");
        var incomeCategory = await CreateCategoryAndGetIdAsync(token, "摘要收入", "Income");
        var expenseCategory = await CreateCategoryAndGetIdAsync(token, "摘要支出", "Expense");

        // Create income 5000
        await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Income", amount = 5000,
            transactionDate = "2025-04-01", categoryId = incomeCategory, accountId,
        });

        // Create expense 3000
        await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 3000,
            transactionDate = "2025-04-02", categoryId = expenseCategory, accountId,
        });

        var response = await AuthorizedGetAsync("/finance/transactions/summary?year=2025&month=4", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("totalIncome").GetDecimal().Should().BeGreaterThanOrEqualTo(5000);
        data.GetProperty("totalExpense").GetDecimal().Should().BeGreaterThanOrEqualTo(3000);
        data.GetProperty("balance").GetDecimal().Should().BeGreaterThanOrEqualTo(2000);
    }

    // ── PUT /finance/transactions/{id} ──────────────

    [Fact]
    public async Task UpdateTransaction_ValidData_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token, "更新帳戶");
        var categoryId = await CreateCategoryAndGetIdAsync(token, "更新分類", "Expense");

        // Create
        var createResp = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 500,
            transactionDate = "2025-04-01", categoryId, accountId,
        });
        var createBody = await ReadBodyAsync(createResp);
        var id = createBody.GetProperty("data").GetProperty("id").GetString()!;

        // Update
        var response = await AuthorizedPutAsync($"/finance/transactions/{id}", token, new
        {
            transactionType = "Expense", amount = 800,
            transactionDate = "2025-04-01", categoryId, accountId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetProperty("amount").GetDecimal().Should().Be(800);
    }

    // ── DELETE /finance/transactions/{id} ────────────

    [Fact]
    public async Task DeleteTransaction_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();
        var accountId = await CreateAccountAndGetIdAsync(token, "刪除帳戶");
        var categoryId = await CreateCategoryAndGetIdAsync(token, "刪除分類", "Expense");

        // Create
        var createResp = await AuthorizedPostAsync("/finance/transactions", token, new
        {
            transactionType = "Expense", amount = 999,
            transactionDate = "2025-04-20", categoryId, accountId,
        });
        var createBody = await ReadBodyAsync(createResp);
        var id = createBody.GetProperty("data").GetProperty("id").GetString()!;

        // Delete
        var deleteResp = await AuthorizedDeleteAsync($"/finance/transactions/{id}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the transaction no longer appears
        var listResp = await AuthorizedGetAsync("/finance/transactions?year=2025&month=4", token);
        var listBody = await ReadBodyAsync(listResp);
        var data = listBody.GetProperty("data");

        // Check that the deleted ID is not present in any daily group
        var found = false;
        foreach (var group in data.EnumerateArray())
        {
            foreach (var txn in group.GetProperty("transactions").EnumerateArray())
            {
                if (txn.GetProperty("id").GetString() == id)
                {
                    found = true;
                }
            }
        }

        found.Should().BeFalse("deleted transaction should not appear in listing");
    }

    // ── Helpers ─────────────────────────────────────

    private async Task<string> CreateAccountAndGetIdAsync(string token, string name = "測試帳戶")
    {
        var resp = await AuthorizedPostAsync("/finance/accounts", token, new
        {
            name,
            accountType = "Cash",
            initialBalance = 10000,
        });
        var body = await ReadBodyAsync(resp);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCategoryAndGetIdAsync(string token, string name = "測試分類", string type = "Expense")
    {
        var resp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name,
            categoryType = type,
        });
        var body = await ReadBodyAsync(resp);
        return body.GetProperty("data").GetProperty("id").GetString()!;
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
