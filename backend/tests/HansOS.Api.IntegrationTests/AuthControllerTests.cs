using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HansOS.Api.IntegrationTests;

public class AuthControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetProperty("accessToken").GetString()
            .Should().NotBeNullOrEmpty();

        // Verify refresh token cookie is set
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.StartsWith("jwt="));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { });

        // Model validation should return 400
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithNoCookie_Returns401()
    {
        var response = await _client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns200()
    {
        var response = await _client.PostAsync("/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetCodes_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/auth/codes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCodes_Authorized_ReturnsPermissionCodes()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/codes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_ReturnsNewAccessToken()
    {
        // First login to get refresh cookie
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204"
        });

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var jwtCookie = cookies.First(c => c.StartsWith("jwt="));

        // Extract cookie value
        var cookieValue = jwtCookie.Split(';')[0]; // "jwt=<value>"

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookieValue);
        var response = await _client.SendAsync(refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newToken = await response.Content.ReadAsStringAsync();
        newToken.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// 已撤銷的 Refresh Token 不可重複使用
    /// </summary>
    [Fact]
    public async Task Refresh_WithRevokedToken_Returns401()
    {
        // 停用自動 Cookie 處理，避免 CookieContainer 覆蓋手動設定的舊 cookie
        var client = CreateNoCookieClient();
        var originalCookie = await LoginAndGetRefreshCookieAsync(client);

        // 正常 refresh 一次（舊 token 被撤銷）
        var firstRefresh = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        firstRefresh.Headers.Add("Cookie", originalCookie);
        var firstResponse = await client.SendAsync(firstRefresh);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — 用已撤銷的舊 cookie 再次嘗試
        var retryRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        retryRequest.Headers.Add("Cookie", originalCookie);
        var retryResponse = await client.SendAsync(retryRequest);

        // Assert
        retryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Refresh 後使用舊 Token 應回傳 401
    /// </summary>
    [Fact]
    public async Task Refresh_ThenUseOldToken_Returns401()
    {
        var client = CreateNoCookieClient();
        var oldCookie = await LoginAndGetRefreshCookieAsync(client);

        // 執行 refresh 取得新 token（舊 token 被撤銷）
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", oldCookie);
        var refreshResponse = await client.SendAsync(refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — 嘗試用舊 token 再次 refresh
        var oldTokenRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        oldTokenRequest.Headers.Add("Cookie", oldCookie);
        var response = await client.SendAsync(oldTokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 登出後使用 Refresh Token 應回傳 401
    /// </summary>
    [Fact]
    public async Task Logout_ThenRefresh_Returns401()
    {
        var client = CreateNoCookieClient();
        var cookie = await LoginAndGetRefreshCookieAsync(client);

        // 登出（撤銷 token）
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        var logoutResponse = await client.SendAsync(logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — 嘗試用已撤銷的 cookie 進行 refresh
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookie);
        var refreshResponse = await client.SendAsync(refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 使用者名稱為 null 時應回傳 400 或 401
    /// </summary>
    [Fact]
    public async Task Login_WithNullUsername_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            password = "x"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(-1);
        body.GetProperty("message").GetString().Should().Be("驗證失敗");
        body.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 密碼為 null 時應回傳 400 或 401
    /// </summary>
    [Fact]
    public async Task Login_WithNullPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "x"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private HttpClient CreateNoCookieClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static async Task<string> LoginAndGetRefreshCookieAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        return cookies.First(c => c.StartsWith("jwt=")).Split(';')[0];
    }
}
