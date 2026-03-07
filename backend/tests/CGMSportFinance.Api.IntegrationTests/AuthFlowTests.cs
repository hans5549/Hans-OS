using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class AuthFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_ReturnsAccessToken_AndSetsRefreshCookie()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "123456" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal(0, payload["code"]!.GetValue<int>());
        Assert.False(string.IsNullOrWhiteSpace(payload["data"]!["accessToken"]!.GetValue<string>()));
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains("refresh_token=", cookies!.Single());
    }

    [Fact]
    public async Task Refresh_RotatesCookie_AndReturnsNewAccessToken()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "vben", password = "123456" });
        var cookie = loginResponse.Headers.GetValues("Set-Cookie").Single();
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';', 2)[0]);

        var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var payload = await ReadJsonAsync(refreshResponse);
        Assert.False(string.IsNullOrWhiteSpace(payload["data"]!["accessToken"]!.GetValue<string>()));
    }

    [Fact]
    public async Task UserInfo_RequiresAuthentication()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/user/info");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MenuAll_ReturnsBackendRouteTree()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var accessToken = await LoginAndGetAccessTokenAsync(client, "admin", "123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/menu/all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadJsonAsync(response);
        Assert.Contains(payload["data"]!.AsArray(), item => item?["path"]?.GetValue<string>() == "/dashboard");
    }

    [Fact]
    public async Task SwaggerJson_IsAvailableInDevelopment()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/auth/login", document);
        Assert.Contains("/api/menu/all", document);
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonNode>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return document!;
    }

    private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var payload = await ReadJsonAsync(response);
        return payload["data"]!["accessToken"]!.GetValue<string>();
    }
}
