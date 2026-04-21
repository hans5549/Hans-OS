using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class PendingRemittanceControllerTests(HansOsWebApplicationFactory factory)
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

    private static HttpRequestMessage AuthPatch(string url, string token, object data)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(data),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static object DefaultCompleteBody() => new
    {
        bankName = "台北富邦",
        transactionDate = "2026-01-15",
    };

    private static HttpRequestMessage AuthDelete(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<Guid> CreateRemittanceAndGetIdAsync(string token, object? data = null)
    {
        data ??= new
        {
            description = "測試待匯款項目",
            amount = 5000,
            sourceAccount = "台北富邦 012-345678",
            targetAccount = "中國信託 987-654321",
        };

        var response = await _client.SendAsync(AuthPost("/pending-remittances", token, data));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    #region GET /pending-remittances

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/pending-remittances");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithoutFilter_ReturnsAllRemittances()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        await CreateRemittanceAndGetIdAsync(token);

        // Act
        var response = await _client.SendAsync(AuthGet("/pending-remittances", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAll_FilterByPendingStatus_ReturnsOnlyPending()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        await CreateRemittanceAndGetIdAsync(token);

        // Act
        var response = await _client.SendAsync(AuthGet("/pending-remittances?status=Pending", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        foreach (var item in data.EnumerateArray())
        {
            item.GetProperty("status").GetInt32().Should().Be(0);
        }
    }

    [Fact]
    public async Task GetAll_FilterByCompletedStatus_ReturnsOnlyCompleted()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);
        await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, DefaultCompleteBody()));

        // Act
        var response = await _client.SendAsync(AuthGet("/pending-remittances?status=Completed", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        foreach (var item in data.EnumerateArray())
        {
            item.GetProperty("status").GetInt32().Should().Be(1);
        }
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();

        // Act — filter by completed, which may yield empty if none exist yet
        var response = await _client.SendAsync(AuthGet("/pending-remittances?status=Completed", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    #endregion

    #region GET /pending-remittances/{id}

    [Fact]
    public async Task GetById_Unauthorized_Returns401()
    {
        // Act
        var response = await _client.GetAsync($"/pending-remittances/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsRemittance()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);

        // Act
        var response = await _client.SendAsync(AuthGet($"/pending-remittances/{id}", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        var data = body.GetProperty("data");
        data.GetProperty("id").GetGuid().Should().Be(id);
        data.GetProperty("description").GetString().Should().Be("測試待匯款項目");
        data.GetProperty("amount").GetDecimal().Should().Be(5000m);
        data.GetProperty("sourceAccount").GetString().Should().Be("台北富邦 012-345678");
        data.GetProperty("targetAccount").GetString().Should().Be("中國信託 987-654321");
        data.GetProperty("status").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();

        // Act
        var response = await _client.SendAsync(AuthGet($"/pending-remittances/{Guid.NewGuid()}", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /pending-remittances

    [Fact]
    public async Task Create_Unauthorized_Returns401()
    {
        // Arrange
        var data = new
        {
            description = "未授權測試",
            amount = 1000,
            sourceAccount = "來源帳戶",
            targetAccount = "目標帳戶",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/pending-remittances", data);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreatedRemittance()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var createData = new
        {
            description = "辦公室租金匯款",
            amount = 35000,
            sourceAccount = "永豐銀行 001-234567",
            targetAccount = "國泰世華 789-012345",
        };

        // Act
        var response = await _client.SendAsync(AuthPost("/pending-remittances", token, createData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("id").GetGuid().Should().NotBeEmpty();
        data.GetProperty("description").GetString().Should().Be("辦公室租金匯款");
        data.GetProperty("amount").GetDecimal().Should().Be(35000m);
        data.GetProperty("sourceAccount").GetString().Should().Be("永豐銀行 001-234567");
        data.GetProperty("targetAccount").GetString().Should().Be("國泰世華 789-012345");
        data.GetProperty("status").GetInt32().Should().Be(0);
        data.GetProperty("createdAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var invalidData = new
        {
            description = string.Empty,
            amount = 0,
            sourceAccount = string.Empty,
            targetAccount = string.Empty,
        };

        // Act
        var response = await _client.SendAsync(AuthPost("/pending-remittances", token, invalidData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithAllOptionalFields_ReturnsCompleteData()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var createData = new
        {
            description = "含完整選填欄位匯款",
            amount = 88000,
            sourceAccount = "兆豐銀行 111-222333",
            targetAccount = "合作金庫 444-555666",
            recipientName = "王大明",
            expectedDate = "2026-03-01",
            note = "每月固定匯款，請確認帳號無誤",
        };

        // Act
        var response = await _client.SendAsync(AuthPost("/pending-remittances", token, createData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("description").GetString().Should().Be("含完整選填欄位匯款");
        data.GetProperty("amount").GetDecimal().Should().Be(88000m);
        data.GetProperty("sourceAccount").GetString().Should().Be("兆豐銀行 111-222333");
        data.GetProperty("targetAccount").GetString().Should().Be("合作金庫 444-555666");
        data.GetProperty("recipientName").GetString().Should().Be("王大明");
        data.GetProperty("expectedDate").GetString().Should().Contain("2026-03-01");
        data.GetProperty("note").GetString().Should().Be("每月固定匯款，請確認帳號無誤");
    }

    [Fact]
    public async Task Create_InvalidDepartment_Returns400()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var invalidData = new
        {
            description = "無效部門匯款",
            amount = 1000,
            sourceAccount = "來源帳戶",
            targetAccount = "目標帳戶",
            departmentId = Guid.NewGuid(),
        };

        // Act
        var response = await _client.SendAsync(AuthPost("/pending-remittances", token, invalidData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /pending-remittances/{id}

    [Fact]
    public async Task Update_Unauthorized_Returns401()
    {
        // Arrange
        var data = new
        {
            description = "未授權更新",
            amount = 1000,
            sourceAccount = "來源帳戶",
            targetAccount = "目標帳戶",
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/pending-remittances/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(data),
        };
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ValidData_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);
        var updateData = new
        {
            description = "已更新的匯款說明",
            amount = 99999,
            sourceAccount = "更新來源帳戶 000-111222",
            targetAccount = "更新目標帳戶 333-444555",
            recipientName = "李小華",
            note = "更新後的備註",
        };

        // Act
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{id}", token, updateData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        // Verify by re-reading
        var getResponse = await _client.SendAsync(AuthGet($"/pending-remittances/{id}", token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = getBody.GetProperty("data");
        data.GetProperty("description").GetString().Should().Be("已更新的匯款說明");
        data.GetProperty("amount").GetDecimal().Should().Be(99999m);
        data.GetProperty("sourceAccount").GetString().Should().Be("更新來源帳戶 000-111222");
        data.GetProperty("targetAccount").GetString().Should().Be("更新目標帳戶 333-444555");
        data.GetProperty("recipientName").GetString().Should().Be("李小華");
        data.GetProperty("note").GetString().Should().Be("更新後的備註");
    }

    [Fact]
    public async Task Update_NonExistingId_Returns404()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var updateData = new
        {
            description = "不存在的匯款",
            amount = 1000,
            sourceAccount = "來源帳戶",
            targetAccount = "目標帳戶",
        };

        // Act
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{Guid.NewGuid()}", token, updateData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /pending-remittances/{id}

    [Fact]
    public async Task Delete_Unauthorized_Returns401()
    {
        // Act
        var response = await _client.DeleteAsync($"/pending-remittances/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);

        // Act
        var response = await _client.SendAsync(AuthDelete($"/pending-remittances/{id}", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        // Verify deletion
        var getResponse = await _client.SendAsync(AuthGet($"/pending-remittances/{id}", token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistingId_Returns404()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();

        // Act
        var response = await _client.SendAsync(AuthDelete($"/pending-remittances/{Guid.NewGuid()}", token));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PUT /pending-remittances/{id}/complete

    [Fact]
    public async Task Complete_Unauthorized_Returns401()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Put, $"/pending-remittances/{Guid.NewGuid()}/complete");
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Complete_PendingRemittance_MarksAsCompleted()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);

        // Act
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, DefaultCompleteBody()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        // Verify completed status
        var getResponse = await _client.SendAsync(AuthGet($"/pending-remittances/{id}", token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = getBody.GetProperty("data");
        data.GetProperty("status").GetInt32().Should().Be(1);
        data.GetProperty("completedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Complete_NonExistingId_Returns404()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();

        // Act
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{Guid.NewGuid()}/complete", token, DefaultCompleteBody()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_Returns400()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);

        // Complete once
        var firstResponse = await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, DefaultCompleteBody()));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — complete again
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, DefaultCompleteBody()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_WithoutBankName_Returns400()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token);

        // Act — missing bankName
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, new
        {
            transactionDate = "2026-01-15",
        }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_WithBankNameAndDate_AutoCreatesExpenseTransaction()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        var id = await CreateRemittanceAndGetIdAsync(token, new
        {
            description = "活動場地費",
            amount = 12000,
            sourceAccount = "台北富邦 001-234567",
            targetAccount = "國泰世華 789-012345",
        });

        // Act
        var response = await _client.SendAsync(AuthPut($"/pending-remittances/{id}/complete", token, new
        {
            bankName = "台北富邦",
            transactionDate = "2026-06-15",
        }));

        // Assert — 200 OK with success code
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        // Assert — remittance status changed to completed
        var remittanceResp = await _client.SendAsync(AuthGet($"/pending-remittances/{id}", token));
        remittanceResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var remittanceBody = await remittanceResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var remittance = remittanceBody.GetProperty("data");
        remittance.GetProperty("status").GetInt32().Should().Be(1);
        remittance.GetProperty("completedAt").GetString().Should().NotBeNullOrEmpty();

        // Assert — a matching BankTransaction was auto-created
        var txResp = await _client.SendAsync(AuthGet("/bank-transactions/台北富邦?year=2026&month=6", token));
        txResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var txBody = await txResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = txBody.GetProperty("data").EnumerateArray().ToList();

        var created = transactions.FirstOrDefault(t =>
            t.GetProperty("pendingRemittanceId").ValueKind != JsonValueKind.Null &&
            t.GetProperty("pendingRemittanceId").GetString() == id.ToString());

        created.ValueKind.Should().NotBe(JsonValueKind.Undefined, because: "待匯款完成後應自動建立收支表支出紀錄");
        created.GetProperty("transactionType").GetInt32().Should().Be(1, because: "自動建立的收支記錄應為支出類型");
        created.GetProperty("amount").GetDecimal().Should().Be(12000m);
        created.GetProperty("description").GetString().Should().Be("活動場地費");
    }

    #endregion
}
