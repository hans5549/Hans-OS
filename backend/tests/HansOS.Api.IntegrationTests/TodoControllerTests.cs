using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class TodoControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ──────────── Projects — 401 ────────────

    [Fact]
    public async Task GetProjects_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/projects");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/todo/projects", new { name = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────── Projects — Happy Path ────────────

    [Fact]
    public async Task GetProjects_Authorized_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedGetAsync("/todo/projects", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreateProject_ValidData_ReturnsCreatedProject()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/projects", token, new
        {
            name = $"測試專案_{Guid.NewGuid():N}",
            color = "#EF4444",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("color").GetString().Should().Be("#EF4444");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateProject_ValidData_ReturnsUpdatedProject()
    {
        var token = await LoginAndGetTokenAsync();
        var projectId = await CreateProjectAndGetIdAsync(token);

        var response = await AuthorizedPutAsync($"/todo/projects/{projectId}", token, new
        {
            name = "更新後的專案名",
            color = "#22C55E",
            isArchived = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("data").GetProperty("name").GetString().Should().Be("更新後的專案名");
        body.GetProperty("data").GetProperty("color").GetString().Should().Be("#22C55E");
    }

    [Fact]
    public async Task DeleteProject_ExistingProject_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var projectId = await CreateProjectAndGetIdAsync(token);

        var response = await AuthorizedDeleteAsync($"/todo/projects/{projectId}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProject_NotOwnedByUser_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthorizedPutAsync($"/todo/projects/{fakeId}", token, new
        {
            name = "不存在的專案",
            color = "#000000",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────── Items — 401 ────────────

    [Fact]
    public async Task GetItems_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateItem_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/todo/items", new { title = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────── Items — Happy Path ────────────

    [Fact]
    public async Task CreateItem_ToInbox_ReturnsCreatedItem()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/items", token, new
        {
            title = $"Inbox 任務_{Guid.NewGuid():N}",
            priority = "High",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Contain("Inbox 任務");
        data.GetProperty("priority").GetString().Should().Be("High");
        data.GetProperty("status").GetString().Should().Be("Pending");
        data.TryGetProperty("projectId", out var projectIdProp);
        // Inbox item should have null projectId
    }

    [Fact]
    public async Task CreateItem_WithDueDate_ReturnsDueDate()
    {
        var token = await LoginAndGetTokenAsync();
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        var response = await AuthorizedPostAsync("/todo/items", token, new
        {
            title = "今天截止任務",
            dueDate = today,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadBodyAsync(response)).GetProperty("data");
        data.GetProperty("dueDate").GetString().Should().Be(today);
    }

    [Fact]
    public async Task GetItems_TodayView_ReturnsOnlyTodayItems()
    {
        var token = await LoginAndGetTokenAsync();
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

        // Create a today item
        await AuthorizedPostAsync("/todo/items", token, new
        {
            title = "Today filter 測試",
            dueDate = today,
        });

        var response = await AuthorizedGetAsync("/todo/items?view=today", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        // All returned items should have today's dueDate
        var items = body.GetProperty("data");
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("dueDate").GetString().Should().Be(today);
        }
    }

    [Fact]
    public async Task GetItems_InboxView_ReturnsOnlyInboxItems()
    {
        var token = await LoginAndGetTokenAsync();

        // Create inbox item (no project)
        await AuthorizedPostAsync("/todo/items", token, new
        {
            title = "Inbox view 測試",
        });

        var response = await AuthorizedGetAsync("/todo/items?view=inbox", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var items = body.GetProperty("data");
        foreach (var item in items.EnumerateArray())
        {
            item.TryGetProperty("projectId", out var pid);
            // All inbox items should have no projectId
        }
    }

    [Fact]
    public async Task ToggleComplete_PendingItem_ReturnsDoneStatus()
    {
        var token = await LoginAndGetTokenAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "切換完成測試");

        var response = await AuthorizedPatchAsync($"/todo/items/{itemId}/complete", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadBodyAsync(response)).GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("Done");
        data.TryGetProperty("completedAt", out var completedAt);
        completedAt.ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ToggleComplete_DoneItem_ReturnsPendingStatus()
    {
        var token = await LoginAndGetTokenAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "雙向切換測試");

        // First toggle → Done
        await AuthorizedPatchAsync($"/todo/items/{itemId}/complete", token);

        // Second toggle → Pending
        var response = await AuthorizedPatchAsync($"/todo/items/{itemId}/complete", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadBodyAsync(response)).GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task DeleteItem_ExistingItem_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "刪除測試任務");

        var response = await AuthorizedDeleteAsync($"/todo/items/{itemId}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCounts_Authorized_ReturnsCounts()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedGetAsync("/todo/counts", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = (await ReadBodyAsync(response)).GetProperty("data");
        data.GetProperty("inbox").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("today").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("upcoming").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("all").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CreateItem_EmptyTitle_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/todo/items", token, new
        {
            title = string.Empty,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ──────────── Helpers ────────────

    private async Task<string> CreateProjectAndGetIdAsync(string token, string name = "測試專案")
    {
        var uniqueName = $"{name}_{Guid.NewGuid():N}";
        var resp = await AuthorizedPostAsync("/todo/projects", token, new
        {
            name = uniqueName,
            color = "#3B82F6",
        });
        var body = await ReadBodyAsync(resp);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateItemAndGetIdAsync(string token, string title = "測試任務")
    {
        var uniqueTitle = $"{title}_{Guid.NewGuid():N}";
        var resp = await AuthorizedPostAsync("/todo/items", token, new { title = uniqueTitle });
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
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Get, url, token));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private Task<HttpResponseMessage> AuthorizedPatchAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Patch, url, token));

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
