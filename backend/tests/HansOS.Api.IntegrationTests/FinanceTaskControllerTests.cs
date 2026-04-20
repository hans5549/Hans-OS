using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class FinanceTaskControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204",
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static HttpRequestMessage AuthGet(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage AuthPost(string url, string token, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage AuthPut(string url, string token, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(data),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage AuthPutNoBody(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static HttpRequestMessage AuthDelete(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<Guid> CreateTaskAndGetIdAsync(string token, object? data = null)
    {
        data ??= new
        {
            title = "測試財務任務",
            priority = 1, // Medium
        };

        var response = await _client.SendAsync(AuthPost("/finance-tasks", token, data));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    #region GET /finance-tasks

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/finance-tasks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthGet("/finance-tasks", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFiltered()
    {
        var token = await LoginAndGetTokenAsync();
        await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthGet("/finance-tasks?status=0", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var tasks = body.GetProperty("data");
        foreach (var task in tasks.EnumerateArray())
        {
            task.GetProperty("status").GetInt32().Should().Be(0); // Pending
        }
    }

    #endregion

    #region GET /finance-tasks/{id}

    [Fact]
    public async Task GetById_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync($"/finance-tasks/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet($"/finance-tasks/{Guid.NewGuid()}", token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ExistingTask_ReturnsTask()
    {
        var token = await LoginAndGetTokenAsync();
        var id = await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthGet($"/finance-tasks/{id}", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetProperty("id").GetGuid().Should().Be(id);
        body.GetProperty("data").GetProperty("title").GetString().Should().Be("測試財務任務");
    }

    #endregion

    #region POST /finance-tasks

    [Fact]
    public async Task Create_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/finance-tasks", new
        {
            title = "test",
            priority = 1,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/finance-tasks", token, new
        {
            priority = 1,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedTask()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/finance-tasks", token, new
        {
            title = "更新財務收支表",
            description = "匯整本月收支資料",
            priority = 0, // High
            dueDate = "2026-02-28",
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("更新財務收支表");
        data.GetProperty("priority").GetInt32().Should().Be(0);
        data.GetProperty("status").GetInt32().Should().Be(0); // Pending
    }

    [Fact]
    public async Task Create_InvalidDepartment_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/finance-tasks", token, new
        {
            title = "測試任務",
            priority = 1,
            departmentId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /finance-tasks/{id}

    [Fact]
    public async Task Update_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/finance-tasks/{Guid.NewGuid()}", new
        {
            title = "test",
            priority = 1,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPut(
            $"/finance-tasks/{Guid.NewGuid()}", token, new
            {
                title = "test",
                priority = 1,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var id = await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthPut(
            $"/finance-tasks/{id}", token, new
            {
                title = "已更新的任務",
                priority = 2, // Low
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update
        var getResponse = await _client.SendAsync(AuthGet($"/finance-tasks/{id}", token));
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetProperty("title").GetString().Should().Be("已更新的任務");
        body.GetProperty("data").GetProperty("priority").GetInt32().Should().Be(2);
    }

    #endregion

    #region DELETE /finance-tasks/{id}

    [Fact]
    public async Task Delete_Unauthorized_Returns401()
    {
        var response = await _client.DeleteAsync($"/finance-tasks/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthDelete(
            $"/finance-tasks/{Guid.NewGuid()}", token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingTask_ReturnsSuccessAndRemoves()
    {
        var token = await LoginAndGetTokenAsync();
        var id = await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthDelete($"/finance-tasks/{id}", token));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted
        var getResponse = await _client.SendAsync(AuthGet($"/finance-tasks/{id}", token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /finance-tasks/{id}/complete

    [Fact]
    public async Task Complete_Unauthorized_Returns401()
    {
        var response = await _client.PutAsync(
            $"/finance-tasks/{Guid.NewGuid()}/complete", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Complete_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPutNoBody(
            $"/finance-tasks/{Guid.NewGuid()}/complete", token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_PendingTask_ReturnsSuccessAndSetsCompleted()
    {
        var token = await LoginAndGetTokenAsync();
        var id = await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthPutNoBody(
            $"/finance-tasks/{id}/complete", token));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify completed
        var getResponse = await _client.SendAsync(AuthGet($"/finance-tasks/{id}", token));
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetProperty("status").GetInt32().Should().Be(2); // Completed
        body.GetProperty("data").GetProperty("completedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var id = await CreateTaskAndGetIdAsync(token);

        // Complete first time
        await _client.SendAsync(AuthPutNoBody($"/finance-tasks/{id}/complete", token));

        // Try to complete again
        var response = await _client.SendAsync(AuthPutNoBody(
            $"/finance-tasks/{id}/complete", token));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /finance-tasks/unified

    [Fact]
    public async Task GetUnified_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/finance-tasks/unified");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnified_Authenticated_ReturnsAggregatedTasks()
    {
        var token = await LoginAndGetTokenAsync();

        // Create a general task
        await CreateTaskAndGetIdAsync(token);

        var response = await _client.SendAsync(AuthGet("/finance-tasks/unified", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("tasks").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetUnified_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        var token = await LoginAndGetTokenAsync();
        await CreateTaskAndGetIdAsync(token);

        // type=0 (General)
        var response = await _client.SendAsync(
            AuthGet("/finance-tasks/unified?type=0", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var tasks = body.GetProperty("data").GetProperty("tasks");
        foreach (var task in tasks.EnumerateArray())
        {
            task.GetProperty("type").GetInt32().Should().Be(0); // General
        }
    }

    [Fact]
    public async Task GetUnified_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var token = await LoginAndGetTokenAsync();
        await CreateTaskAndGetIdAsync(token);

        // status=0 (Pending)
        var response = await _client.SendAsync(
            AuthGet("/finance-tasks/unified?status=0", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var tasks = body.GetProperty("data").GetProperty("tasks");
        foreach (var task in tasks.EnumerateArray())
        {
            task.GetProperty("status").GetInt32().Should().Be(0); // Pending
        }
    }

    [Fact]
    public async Task GetUnified_WithYearMonth_ReturnsFilteredResults()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await _client.SendAsync(
            AuthGet("/finance-tasks/unified?year=2026&month=1", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetProperty("tasks").ValueKind.Should().Be(JsonValueKind.Array);
    }

    #endregion
}
