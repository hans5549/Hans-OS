using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class ArticleCollectionControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetBookmarks_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/article-collection");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBookmark_ExternalUrl_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/designing-bookmarks",
            title = "Designing Bookmarks",
            tags = new[] { "design", "ux" },
            isPinned = true,
            isRead = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");

        data.GetProperty("sourceType").GetString().Should().Be("ExternalUrl");
        data.GetProperty("url").GetString().Should().Be("https://example.com/articles/designing-bookmarks");
        data.GetProperty("domain").GetString().Should().Be("example.com");
        data.GetProperty("isPinned").GetBoolean().Should().BeTrue();
        data.GetProperty("tags").EnumerateArray().Select(item => item.GetString()).Should().BeEquivalentTo(["design", "ux"]);
    }

    [Fact]
    public async Task CreateBookmark_InternalArticle_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "InternalArticle",
            sourceId = "article-123",
            title = "Hans-OS 內部文章",
            isPinned = false,
            isRead = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBookmark_DuplicateExternalUrl_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var payload = new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/duplicate",
            title = "Duplicate",
        };

        var first = await AuthorizedPostAsync("/article-collection", token, payload);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await AuthorizedPostAsync("/article-collection", token, payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGroup_ThenCreateBookmarkWithGroup_ReturnsGroupName()
    {
        var token = await LoginAndGetTokenAsync();
        var groupResponse = await AuthorizedPostAsync("/article-collection/groups", token, new
        {
            name = "待讀清單",
            sortOrder = 1,
        });
        var groupBody = await ReadBodyAsync(groupResponse);
        var groupId = groupBody.GetProperty("data").GetProperty("id").GetString();

        var bookmarkResponse = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/grouped",
            title = "Grouped Article",
            groupId,
            tags = new[] { "obsidian", "research" },
        });

        bookmarkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookmarkBody = await ReadBodyAsync(bookmarkResponse);
        var data = bookmarkBody.GetProperty("data");

        data.GetProperty("groupId").GetString().Should().Be(groupId);
        data.GetProperty("groupName").GetString().Should().Be("待讀清單");
    }

    [Fact]
    public async Task GetBookmarks_FilteredByPinnedAndKeyword_ReturnsExpectedItems()
    {
        var token = await LoginAndGetTokenAsync();

        await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/pinned-match",
            title = "Obsidian Bookmark Notes",
            isPinned = true,
        });

        await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/other",
            title = "Other Article",
            isPinned = false,
        });

        var response = await AuthorizedGetAsync(
            "/article-collection?keyword=Obsidian&isPinned=true",
            token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var items = body.GetProperty("data").GetProperty("items").EnumerateArray().ToList();

        items.Should().HaveCount(1);
        items[0].GetProperty("title").GetString().Should().Be("Obsidian Bookmark Notes");
    }

    [Fact]
    public async Task PatchBookmarkState_ValidData_ReturnsUpdatedState()
    {
        var token = await LoginAndGetTokenAsync();
        var createResponse = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/state",
            title = "Stateful",
        });
        var createBody = await ReadBodyAsync(createResponse);
        var bookmarkId = createBody.GetProperty("data").GetProperty("id").GetString();

        var patchResponse = await AuthorizedPatchAsync($"/article-collection/{bookmarkId}/state", token, new
        {
            isPinned = true,
            isRead = true,
        });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchBody = await ReadBodyAsync(patchResponse);
        var data = patchBody.GetProperty("data");

        data.GetProperty("isPinned").GetBoolean().Should().BeTrue();
        data.GetProperty("isRead").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DeleteGroup_ClearsBookmarkGroupReference()
    {
        var token = await LoginAndGetTokenAsync();
        var groupResponse = await AuthorizedPostAsync("/article-collection/groups", token, new
        {
            name = "要刪除的群組",
        });
        var groupBody = await ReadBodyAsync(groupResponse);
        var groupId = groupBody.GetProperty("data").GetProperty("id").GetString();

        var bookmarkResponse = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/clear-group",
            title = "Clear Group",
            groupId,
        });
        var bookmarkBody = await ReadBodyAsync(bookmarkResponse);
        var bookmarkId = bookmarkBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await AuthorizedDeleteAsync($"/article-collection/groups/{groupId}", token);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await AuthorizedGetAsync("/article-collection", token);
        var getBody = await ReadBodyAsync(getResponse);
        var bookmark = getBody.GetProperty("data")
            .GetProperty("items")
            .EnumerateArray()
            .First(item => item.GetProperty("id").GetString() == bookmarkId);

        bookmark.GetProperty("groupId").ValueKind.Should().Be(JsonValueKind.Null);
        bookmark.GetProperty("groupName").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task DeleteBookmark_RemovesItem()
    {
        var token = await LoginAndGetTokenAsync();
        var createResponse = await AuthorizedPostAsync("/article-collection", token, new
        {
            sourceType = "ExternalUrl",
            url = "https://example.com/articles/delete-me",
            title = "Delete Me",
        });
        var createBody = await ReadBodyAsync(createResponse);
        var bookmarkId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await AuthorizedDeleteAsync($"/article-collection/{bookmarkId}", token);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await AuthorizedGetAsync("/article-collection", token);
        var getBody = await ReadBodyAsync(getResponse);
        getBody.GetProperty("data")
            .GetProperty("items")
            .EnumerateArray()
            .Should()
            .NotContain(item => item.GetProperty("id").GetString() == bookmarkId);
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

    private Task<HttpResponseMessage> AuthorizedPatchAsync(string url, string token, object data)
        => _client.SendAsync(CreateAuthorizedRequest(new HttpMethod("PATCH"), url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
