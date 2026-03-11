using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CGMSportFinance.Api.IntegrationTests;

public sealed class ProfileFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetProfile_ReturnsDatabaseBackedProfile()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var accessToken = await LoginAndGetAccessTokenAsync(client, "admin", "123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("Test Admin", payload["data"]!["header"]!["realName"]!.GetValue<string>());
        Assert.Equal("admin", payload["data"]!["basic"]!["username"]!.GetValue<string>());
        Assert.True(payload["data"]!["notifications"]!["notifySystemMessage"]!.GetValue<bool>());
    }

    [Fact]
    public async Task UpdateBasic_PersistsProfileChanges()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var accessToken = await LoginAndGetAccessTokenAsync(client, "admin", "123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateResponse = await client.PutAsJsonAsync("/api/profile/basic", new
        {
            realName = "Admin Updated",
            introduction = "Updated from integration test",
            email = "admin-updated@example.local",
            phoneNumber = "0912345678",
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var profileResponse = await client.GetAsync("/api/profile");
        var payload = await ReadJsonAsync(profileResponse);

        Assert.Equal("Admin Updated", payload["data"]!["basic"]!["realName"]!.GetValue<string>());
        Assert.Equal("Updated from integration test", payload["data"]!["basic"]!["introduction"]!.GetValue<string>());
        Assert.Equal("admin-updated@example.local", payload["data"]!["basic"]!["email"]!.GetValue<string>());
        Assert.Equal("0912345678", payload["data"]!["basic"]!["phoneNumber"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateNotifications_PersistsNotificationPreferences()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var accessToken = await LoginAndGetAccessTokenAsync(client, "jack", "123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync("/api/profile/notifications", new
        {
            notifyAccountPassword = false,
            notifySystemMessage = true,
            notifyTodoTask = false,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profileResponse = await client.GetAsync("/api/profile");
        var payload = await ReadJsonAsync(profileResponse);

        Assert.False(payload["data"]!["notifications"]!["notifyAccountPassword"]!.GetValue<bool>());
        Assert.True(payload["data"]!["notifications"]!["notifySystemMessage"]!.GetValue<bool>());
        Assert.False(payload["data"]!["notifications"]!["notifyTodoTask"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPassword_AndAllowsRelogin()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var accessToken = await LoginAndGetAccessTokenAsync(client, "vben", "123456");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changeResponse = await client.PostAsJsonAsync("/api/profile/change-password", new
        {
            oldPassword = "123456",
            newPassword = "654321",
            confirmPassword = "654321",
        });

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "vben", password = "123456" });
        Assert.Equal(HttpStatusCode.Forbidden, oldLoginResponse.StatusCode);

        var newLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "vben", password = "654321" });
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
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
