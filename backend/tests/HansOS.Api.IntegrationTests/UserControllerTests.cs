using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class UserControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetInfo_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/user/info");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetInfo_Authorized_ReturnsUserInfo()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/user/info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("username").GetString().Should().Be("hans");
        data.GetProperty("realName").GetString().Should().Be("Hans");
        data.GetProperty("roles").GetArrayLength().Should().BeGreaterThan(0);
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
}
