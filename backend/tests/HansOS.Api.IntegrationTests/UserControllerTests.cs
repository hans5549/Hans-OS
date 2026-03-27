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
        data.GetProperty("phone").GetString().Should().NotBeNull();
        data.GetProperty("desc").GetString().Should().NotBeNull();
        data.GetProperty("avatar").GetString().Should().NotBeNull();
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

        // Update profile with all fields
        var updateRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new
            {
                realName = "Hans Updated",
                email = "hans.updated@example.com",
                phone = "0912345678",
                avatar = "https://example.com/avatar.png",
                desc = "自我介紹測試"
            })
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify via GET /user/info
        var infoRequest = new HttpRequestMessage(HttpMethod.Get, "/user/info");
        infoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var infoResponse = await _client.SendAsync(infoRequest);
        var body = await infoResponse.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("realName").GetString().Should().Be("Hans Updated");
        data.GetProperty("email").GetString().Should().Be("hans.updated@example.com");
        data.GetProperty("phone").GetString().Should().Be("0912345678");
        data.GetProperty("avatar").GetString().Should().Be("https://example.com/avatar.png");
        data.GetProperty("desc").GetString().Should().Be("自我介紹測試");

        // Restore original values
        var restoreRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans" })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidEmail_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans", email = "not-an-email" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithTooLongDesc_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans", desc = new string('A', 501) })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    [Fact]
    public async Task ChangePassword_ExactlyMinLength8_Returns200()
    {
        const string originalPassword = "H@ns19951204";
        const string minLengthPassword = "Aa1!xxxx"; // 剛好 8 字元，符合所有複雜度規則

        var token = await LoginAndGetTokenAsync();

        // 變更為最短密碼
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = originalPassword, newPassword = minLengthPassword })
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var changeResponse = await _client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 還原密碼
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = minLengthPassword
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newToken = loginBody.GetProperty("data").GetProperty("accessToken").GetString()!;

        var restoreRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = minLengthPassword, newPassword = originalPassword })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task ChangePassword_ExactlyMaxLength128_Returns200()
    {
        const string originalPassword = "H@ns19951204";
        // 剛好 128 字元：Aa1! + 124 個 'x'
        var maxLengthPassword = "Aa1!" + new string('x', 124);

        var token = await LoginAndGetTokenAsync();

        // 變更為最長密碼
        var changeRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = originalPassword, newPassword = maxLengthPassword })
        };
        changeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var changeResponse = await _client.SendAsync(changeRequest);
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 還原密碼
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = maxLengthPassword
        });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var newToken = loginBody.GetProperty("data").GetProperty("accessToken").GetString()!;

        var restoreRequest = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = maxLengthPassword, newPassword = originalPassword })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task ChangePassword_OldAndNewSame_Returns200()
    {
        // Identity 預設不阻擋新舊密碼相同，驗證此行為
        const string originalPassword = "H@ns19951204";

        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/user/change-password")
        {
            Content = JsonContent.Create(new { oldPassword = originalPassword, newPassword = originalPassword })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region UpdateProfile (Boundary)

    [Fact]
    public async Task UpdateProfile_PartialFieldsOnly_PreservesOthers()
    {
        var token = await LoginAndGetTokenAsync();

        // 先設定所有欄位為已知值
        var setupRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new
            {
                realName = "Original Name",
                email = "original@example.com",
                phone = "0911222333",
                avatar = "https://example.com/original.png",
                desc = "原始描述"
            })
        };
        setupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var setupResponse = await _client.SendAsync(setupRequest);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 只更新 RealName，其餘欄位不傳（null）
        var partialRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Updated Name" })
        };
        partialRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var partialResponse = await _client.SendAsync(partialRequest);
        partialResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證 RealName 已更新，其餘欄位保持不變
        var infoRequest = new HttpRequestMessage(HttpMethod.Get, "/user/info");
        infoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var infoResponse = await _client.SendAsync(infoRequest);
        var body = await infoResponse.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        data.GetProperty("realName").GetString().Should().Be("Updated Name");
        data.GetProperty("email").GetString().Should().Be("original@example.com");
        data.GetProperty("phone").GetString().Should().Be("0911222333");
        data.GetProperty("avatar").GetString().Should().Be("https://example.com/original.png");
        data.GetProperty("desc").GetString().Should().Be("原始描述");

        // 還原
        var restoreRequest = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans" })
        };
        restoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_InvalidPhone_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans", phone = "abc-not-phone" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_InvalidAvatarUrl_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Put, "/user/profile")
        {
            Content = JsonContent.Create(new { realName = "Hans", avatar = "not-a-url" })
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
