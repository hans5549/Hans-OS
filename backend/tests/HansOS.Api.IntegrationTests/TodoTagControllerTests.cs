using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class TodoTagControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetTags ─────────────────────────────────────

    [Fact]
    public async Task GetTags_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/tags");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTags_Authenticated_ReturnsValidResponse()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedGetAsync("/todo/tags", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetTags_WithData_ReturnsSortedByName()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/todo/tags", token, new { name = "Z標籤" });
        await AuthorizedPostAsync("/todo/tags", token, new { name = "A標籤" });

        var response = await AuthorizedGetAsync("/todo/tags", token);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        var names = new List<string>();
        foreach (var item in data.EnumerateArray())
        {
            names.Add(item.GetProperty("name").GetString()!);
        }

        var indexA = names.IndexOf("A標籤");
        var indexZ = names.IndexOf("Z標籤");
        indexA.Should().BeLessThan(indexZ, "按名稱排序，A 在 Z 前面");
    }

    // ── CreateTag ───────────────────────────────────

    [Fact]
    public async Task CreateTag_ValidData_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/tags", token, new
        {
            name = "新標籤",
            color = "#3498DB",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("新標籤");
        data.GetProperty("color").GetString().Should().Be("#3498DB");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTag_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/todo/tags", new { name = "未授權標籤" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTag_EmptyName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/tags", token, new { name = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new { name = "重複標籤測試" };

        var first = await AuthorizedPostAsync("/todo/tags", token, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await AuthorizedPostAsync("/todo/tags", token, payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── UpdateTag ───────────────────────────────────

    [Fact]
    public async Task UpdateTag_ValidData_ReturnsUpdated()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/todo/tags", token, new { name = "待更新標籤" });
        var createBody = await ReadBodyAsync(createResp);
        var tagId = createBody.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/todo/tags/{tagId}", token, new
        {
            name = "已更新標籤",
            color = "#FF0000",
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        updateBody.GetProperty("data").GetProperty("name").GetString().Should().Be("已更新標籤");
        updateBody.GetProperty("data").GetProperty("color").GetString().Should().Be("#FF0000");
    }

    [Fact]
    public async Task UpdateTag_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthorizedPutAsync($"/todo/tags/{fakeId}", token, new { name = "不存在標籤" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTag_DuplicateName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/todo/tags", token, new { name = "標籤甲" });
        var resp2 = await AuthorizedPostAsync("/todo/tags", token, new { name = "標籤乙" });
        var body2 = await ReadBodyAsync(resp2);
        var id2 = body2.GetProperty("data").GetProperty("id").GetString();

        var updateResp = await AuthorizedPutAsync($"/todo/tags/{id2}", token, new { name = "標籤甲" });
        updateResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DeleteTag ───────────────────────────────────

    [Fact]
    public async Task DeleteTag_Exists_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/todo/tags", token, new { name = "待刪除標籤" });
        var createBody = await ReadBodyAsync(createResp);
        var tagId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResp = await AuthorizedDeleteAsync($"/todo/tags/{tagId}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 確認已刪除
        var getResp = await AuthorizedGetAsync("/todo/tags", token);
        var getBody = await ReadBodyAsync(getResp);
        var found = false;
        foreach (var tag in getBody.GetProperty("data").EnumerateArray())
        {
            if (tag.GetProperty("id").GetString() == tagId) found = true;
        }
        found.Should().BeFalse("標籤應已被刪除");
    }

    [Fact]
    public async Task DeleteTag_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthorizedDeleteAsync($"/todo/tags/{fakeId}", token);
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
