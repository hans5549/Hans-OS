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

    #region GetInfo

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
        data.GetProperty("email").GetString().Should().NotBeNull();
    }

    #endregion

    #region UpdateProfile

    [Fact]
    public async Task UpdateProfile_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/user/profile", new { realName = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_Returns200()
    {
        var token = await LoginAndGetTokenAsync();

        // Update profile
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans Updated" })
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify via GET /user/info
        var infoRequest = new HttpRequestMessage(HttpMethod.Get, "/user/info");
        infoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var infoResponse = await _client.SendAsync(infoRequest);
        var body = await infoResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("realName").GetString().Should().Be("Hans Updated");

        // Restore original name
        var restoreRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans" })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyRealName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithTooLongRealName_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = new string('A', 101) })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/user/change-password",
            new { oldPassword = "test", newPassword = "test1234" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_Returns200()
    {
        const string originalPassword = "H@ns19951204";
        const string newPassword = "N3wP@ssw0rd!";

        var token = await LoginAndGetTokenAsync();

        // Change password
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = originalPassword, newPassword })
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var changeResponse = await _client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify new password works by logging in
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = newPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original password
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newToken = loginBody.GetProperty("data").GetProperty("accessToken").GetString()!;

        var restoreRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = newPassword, newPassword = originalPassword })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = "WrongP@ss1", newPassword = "N3wP@ssw0rd!" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        // "weakpass" — no uppercase, no digit, no special char
        var request = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = "H@ns19951204", newPassword = "weakpass" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify error message is translated to Traditional Chinese
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errorMessage = body.GetProperty("error").GetString()!;
        errorMessage.Should().Contain("密碼");
    }

    [Fact]
    public async Task ChangePassword_WithTooLongPassword_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = "H@ns19951204", newPassword = new string('A', 129) })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

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
