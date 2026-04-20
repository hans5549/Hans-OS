using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class TodoCategoryControllerTests(HansOsWebApplicationFactory factory)
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
        var response = await _client.GetAsync("/todo/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_Authenticated_ReturnsValidResponse()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedGetAsync("/todo/categories", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetCategories_WithData_ReturnsSortedList()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "分類B_排序",
            sortOrder = 2,
        });
        await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "分類A_排序",
            sortOrder = 1,
        });

        var response = await AuthorizedGetAsync("/todo/categories", token);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");

        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        // 驗證排序：先按 SortOrder
        var names = new List<string>();
        foreach (var item in data.EnumerateArray())
        {
            names.Add(item.GetProperty("name").GetString()!);
        }

        var indexA = names.IndexOf("分類A_排序");
        var indexB = names.IndexOf("分類B_排序");
        indexA.Should().BeLessThan(indexB, "SortOrder 小的應排在前面");
    }

    // ── CreateCategory ──────────────────────────────

    [Fact]
    public async Task CreateCategory_ValidData_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "新分類",
            color = "#FF5733",
            icon = "mdi:folder",
            sortOrder = 1,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("新分類");
        data.GetProperty("color").GetString().Should().Be("#FF5733");
        data.GetProperty("icon").GetString().Should().Be("mdi:folder");
        data.GetProperty("sortOrder").GetInt32().Should().Be(1);
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCategory_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/todo/categories", new
        {
            name = "未授權分類",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_EmptyName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new { name = "重複分類測試_Todo" };

        var first = await AuthorizedPostAsync("/todo/categories", token, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await AuthorizedPostAsync("/todo/categories", token, payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── UpdateCategory ──────────────────────────────

    [Fact]
    public async Task UpdateCategory_ValidData_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "待更新分類",
        });
        var createBody = await ReadBodyAsync(createResp);
        var categoryId = createBody.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/todo/categories/{categoryId}", token, new
        {
            name = "已更新分類",
            color = "#00FF00",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("已更新分類");
        updateBody.GetProperty("data").GetProperty("color").GetString().Should().Be("#00FF00");
    }

    [Fact]
    public async Task UpdateCategory_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthorizedPutAsync($"/todo/categories/{fakeId}", token, new
        {
            name = "不存在分類",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/todo/categories", token, new { name = "分類甲_Update" });
        var resp2 = await AuthorizedPostAsync("/todo/categories", token, new { name = "分類乙_Update" });
        var body2 = await ReadBodyAsync(resp2);
        var id2 = body2.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/todo/categories/{id2}", token, new
        {
            name = "分類甲_Update",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DeleteCategory ──────────────────────────────

    [Fact]
    public async Task DeleteCategory_NoTodoItems_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/todo/categories", token, new
        {
            name = "待刪除分類_Todo",
        });
        var createBody = await ReadBodyAsync(createResp);
        var categoryId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResp = await AuthorizedDeleteAsync($"/todo/categories/{categoryId}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 確認已刪除
        var getResp = await AuthorizedGetAsync("/todo/categories", token);
        var getBody = await ReadBodyAsync(getResp);
        var found = false;
        foreach (var cat in getBody.GetProperty("data").EnumerateArray())
        {
            if (cat.GetProperty("id").GetString() == categoryId) found = true;
        }
        found.Should().BeFalse("分類應已被刪除");
    }

    [Fact]
    public async Task DeleteCategory_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthorizedDeleteAsync($"/todo/categories/{fakeId}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Get, url, token));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
