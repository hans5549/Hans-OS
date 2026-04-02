using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class TransactionCategoryControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetCategories ───────────────────────────────

    [Fact]
    public async Task GetCategories_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/finance/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_Default_ReturnsSystemCategories()
    {
        var token = await LoginAndGetTokenAsync();
        var userId = await GetUserIdAsync(token);

        // Seed system categories directly (InMemory DB doesn't run EF migrations)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.TransactionCategories.AddRange(
            new TransactionCategory
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Name = "飲食",
                CategoryType = CategoryType.Expense,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new TransactionCategory
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Name = "薪資",
                CategoryType = CategoryType.Income,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var response = await AuthorizedGetAsync("/finance/categories", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetCategories_FilterExpense_ReturnsOnlyExpense()
    {
        var token = await LoginAndGetTokenAsync();

        // Create both types via API
        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "篩選支出分類",
            categoryType = "Expense",
        });
        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "篩選收入分類",
            categoryType = "Income",
        });

        var response = await AuthorizedGetAsync("/finance/categories?type=Expense", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");

        foreach (var category in data.EnumerateArray())
        {
            category.GetProperty("categoryType").GetString().Should().Be("Expense");
        }
    }

    [Fact]
    public async Task GetCategories_FilterIncome_ReturnsOnlyIncome()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "收入篩選支出",
            categoryType = "Expense",
        });
        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "收入篩選收入",
            categoryType = "Income",
        });

        var response = await AuthorizedGetAsync("/finance/categories?type=Income", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");

        foreach (var category in data.EnumerateArray())
        {
            category.GetProperty("categoryType").GetString().Should().Be("Income");
        }
    }

    // ── CreateCategory ──────────────────────────────

    [Fact]
    public async Task CreateCategory_MainCategory_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "測試分類",
            categoryType = "Expense",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("測試分類");
        data.GetProperty("categoryType").GetString().Should().Be("Expense");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCategory_SubCategory_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();

        // Create main category
        var mainResp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "父分類",
            categoryType = "Expense",
        });
        var mainBody = await ReadBodyAsync(mainResp);
        var parentId = mainBody.GetProperty("data").GetProperty("id").GetString();

        // Create sub category
        var subResp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "子分類",
            categoryType = "Expense",
            parentId,
        });

        subResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var subBody = await ReadBodyAsync(subResp);
        subBody.GetProperty("code").GetInt32().Should().Be(0);
        subBody.GetProperty("data").GetProperty("name").GetString().Should().Be("子分類");

        // Verify tree structure via GET
        var getResp = await AuthorizedGetAsync("/finance/categories?type=Expense", token);
        var getBody = await ReadBodyAsync(getResp);
        var categories = getBody.GetProperty("data");

        var foundParent = false;
        foreach (var cat in categories.EnumerateArray())
        {
            if (cat.GetProperty("id").GetString() == parentId)
            {
                foundParent = true;
                var children = cat.GetProperty("children");
                children.ValueKind.Should().NotBe(JsonValueKind.Null);
                children.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

                var foundChild = false;
                foreach (var child in children.EnumerateArray())
                {
                    if (child.GetProperty("name").GetString() == "子分類")
                    {
                        foundChild = true;
                    }
                }

                foundChild.Should().BeTrue("子分類應出現在父分類的 children 中");
            }
        }

        foundParent.Should().BeTrue("父分類應出現在分類清單中");
    }

    // ── UpdateCategory ──────────────────────────────

    [Fact]
    public async Task UpdateCategory_ValidData_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "更新前分類",
            categoryType = "Expense",
        });
        var createBody = await ReadBodyAsync(createResp);
        var categoryId = createBody.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/finance/categories/{categoryId}", token, new
        {
            name = "更新後分類",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("更新後分類");
    }

    // ── DeleteCategory ──────────────────────────────

    [Fact]
    public async Task DeleteCategory_NoTransactions_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "待刪除分類",
            categoryType = "Income",
        });
        var createBody = await ReadBodyAsync(createResp);
        var categoryId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResp = await AuthorizedDeleteAsync($"/finance/categories/{categoryId}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify removed
        var getResp = await AuthorizedGetAsync("/finance/categories?type=Income", token);
        var getBody = await ReadBodyAsync(getResp);
        var categories = getBody.GetProperty("data");

        var found = false;
        foreach (var cat in categories.EnumerateArray())
        {
            if (cat.GetProperty("id").GetString() == categoryId)
            {
                found = true;
            }
        }

        found.Should().BeFalse("分類應已被刪除");
    }

    // ── Duplicate / Tree ────────────────────────────

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new
        {
            name = "重複分類測試",
            categoryType = "Expense",
        };

        var first = await AuthorizedPostAsync("/finance/categories", token, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await AuthorizedPostAsync("/finance/categories", token, payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCategories_TreeStructure_HasChildren()
    {
        var token = await LoginAndGetTokenAsync();

        // Create main category
        var mainResp = await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "樹狀主分類",
            categoryType = "Expense",
        });
        var mainBody = await ReadBodyAsync(mainResp);
        var parentId = mainBody.GetProperty("data").GetProperty("id").GetString();

        // Create two sub categories
        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "樹狀子分類A",
            categoryType = "Expense",
            parentId,
        });
        await AuthorizedPostAsync("/finance/categories", token, new
        {
            name = "樹狀子分類B",
            categoryType = "Expense",
            parentId,
        });

        var getResp = await AuthorizedGetAsync("/finance/categories?type=Expense", token);
        var getBody = await ReadBodyAsync(getResp);
        var categories = getBody.GetProperty("data");

        var foundParent = false;
        foreach (var cat in categories.EnumerateArray())
        {
            if (cat.GetProperty("id").GetString() == parentId)
            {
                foundParent = true;
                var children = cat.GetProperty("children");
                children.ValueKind.Should().NotBe(JsonValueKind.Null);
                children.GetArrayLength().Should().Be(2);

                var childNames = new List<string>();
                foreach (var child in children.EnumerateArray())
                {
                    childNames.Add(child.GetProperty("name").GetString()!);
                }

                childNames.Should().Contain("樹狀子分類A");
                childNames.Should().Contain("樹狀子分類B");
            }
        }

        foundParent.Should().BeTrue("主分類應出現在分類清單中");
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

    private async Task<string> GetUserIdAsync(string token)
    {
        var response = await AuthorizedGetAsync("/user/info", token);
        var body = await ReadBodyAsync(response);
        return body.GetProperty("data").GetProperty("userId").GetString()!;
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
