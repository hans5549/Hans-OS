using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using ClosedXML.Excel;

using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class BankTransactionControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ── Authentication Helpers ──────────────────────────────

    private async Task<string> LoginAndGetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private static HttpRequestMessage AuthGet(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthPost(string url, string token, object data)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(data),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthPut(string url, string token, object data)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(data),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpRequestMessage AuthDelete(string url, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    private static JsonElement GetData(JsonElement body) => body.GetProperty("data");

    private async Task<string> CreateDepartmentAsync(string token, string name)
    {
        var response = await _client.SendAsync(
            AuthPost("/tsf-settings/departments", token, new { name }));
        return GetData(await ReadBodyAsync(response)).GetProperty("id").GetString()!;
    }

    private async Task<string> CreateTransactionAsync(string token, string bankName, object data)
    {
        var response = await _client.SendAsync(AuthPost($"/bank-transactions/{bankName}", token, data));
        return GetData(await ReadBodyAsync(response)).GetProperty("id").GetString()!;
    }

    // ── GET Transactions ────────────────────────────────────

    [Fact]
    public async Task GetTransactions_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTransactions_EmptyMonth_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetTransactions_FullYear_ReturnsAllTransactions()
    {
        var token = await LoginAndGetTokenAsync();

        // Create two transactions in different months
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-01-15",
            description = "一月收入",
            amount = 1000,
        }));
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2026-06-15",
            description = "六月支出",
            amount = 500,
        }));

        // Get full year (no month parameter)
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    // ── GET Summary ─────────────────────────────────────────

    [Fact]
    public async Task GetSummary_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行/summary?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_EmptyMonth_ReturnsZeroSummary()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行/summary?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("totalIncome").GetDecimal().Should().Be(0);
        data.GetProperty("totalExpense").GetDecimal().Should().Be(0);
    }

    // ── POST Create ─────────────────────────────────────────

    [Fact]
    public async Task CreateTransaction_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/bank-transactions/上海銀行", new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTransaction_ValidIncome_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000,
            fee = 15,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        data.GetProperty("bankName").GetString().Should().Be("上海銀行");
        data.GetProperty("description").GetString().Should().Be("會費收入");
        data.GetProperty("amount").GetDecimal().Should().Be(5000m);
        data.GetProperty("fee").GetDecimal().Should().Be(15m);
        // 收入不追蹤收據，應為 false
        data.GetProperty("receiptCollected").GetBoolean().Should().BeFalse();
        data.GetProperty("receiptMailed").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateTransaction_InvalidDepartment_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "會費收入",
            amount = 5000,
            departmentId = Guid.NewGuid(),
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_MissingDescription_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "",
            amount = 5000,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT Update ──────────────────────────────────────────

    [Fact]
    public async Task UpdateTransaction_ValidData_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync();

        // Create first
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/合作金庫", token, new
        {
            transactionType = 1,
            transactionDate = "2026-04-01",
            description = "場地租金",
            amount = 2000,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Update
        var updateResponse = await _client.SendAsync(AuthPut($"/bank-transactions/{id}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-04-02",
            description = "場地租金（更新）",
            amount = 2500,
        }));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/合作金庫?year=2026&month=4", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = getBody.GetProperty("data");
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var updated = transactions[0];
        updated.GetProperty("description").GetString().Should().Be("場地租金（更新）");
        updated.GetProperty("amount").GetDecimal().Should().Be(2500m);
    }

    [Fact]
    public async Task UpdateTransaction_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPut($"/bank-transactions/{Guid.NewGuid()}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "不存在",
            amount = 100,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_ValidId_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync();

        // Create
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2026-05-01",
            description = "待刪除",
            amount = 100,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // Delete
        var deleteResponse = await _client.SendAsync(AuthDelete($"/bank-transactions/{id}", token));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify gone
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2026&month=5", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        getBody.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task DeleteTransaction_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthDelete($"/bank-transactions/{Guid.NewGuid()}", token));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Running Balance Correctness ─────────────────────────

    [Fact]
    public async Task RunningBalance_CalculatesCorrectlyWithFees()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "合作金庫";

        // Create transactions in order
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-07-01",
            description = "收入1",
            amount = 10000,
            fee = 0,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-07-05",
            description = "支出1",
            amount = 3000,
            fee = 15,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-07-10",
            description = "收入2",
            amount = 5000,
            fee = 10,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}?year=2026&month=7", token));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(3);

        // Opening balance is 0 (no initial balance seeded for in-memory test)
        // After income 10000, fee 0: balance = 0 + 10000 - 0 = 10000
        data[0].GetProperty("runningBalance").GetDecimal().Should().Be(10000m);
        // After expense 3000, fee 15: balance = 10000 - 3000 - 15 = 6985
        data[1].GetProperty("runningBalance").GetDecimal().Should().Be(6985m);
        // After income 5000, fee 10: balance = 6985 + 5000 - 10 = 11975
        data[2].GetProperty("runningBalance").GetDecimal().Should().Be(11975m);
    }

    [Fact]
    public async Task Summary_CalculatesCorrectly()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "上海銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-08-01",
            description = "收入A",
            amount = 20000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-08-10",
            description = "收入B",
            amount = 5000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-08-15",
            description = "支出A",
            amount = 8000,
            fee = 30,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/summary?year=2026&month=8", token));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");

        data.GetProperty("totalIncome").GetDecimal().Should().Be(25000m);
        // totalExpense = expense amounts + all fees = 8000 + 30 = 8030
        data.GetProperty("totalExpense").GetDecimal().Should().Be(8030m);
    }

    // ── GET Summary (Happy Path) ───────────────────────────────

    [Fact]
    public async Task GetSummary_WithData_ReturnsCorrectSummary()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "合作金庫";

        // 建立收入與支出（使用不同銀行避免其他測試資料干擾）
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2026-09-01",
            description = "會費收入",
            amount = 15000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2026-09-15",
            description = "場地費",
            amount = 3000,
            fee = 20,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/summary?year=2026&month=9", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("totalIncome").GetDecimal().Should().Be(15000m);
        data.GetProperty("totalExpense").GetDecimal().Should().Be(3020m);
    }

    // ── PUT Update (Unauthorized) ───────────────────────────

    [Fact]
    public async Task UpdateTransaction_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync($"/bank-transactions/{Guid.NewGuid()}", new
        {
            transactionType = 0,
            transactionDate = "2026-03-01",
            description = "test",
            amount = 100,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DELETE (Unauthorized) ────────────────────────────────

    [Fact]
    public async Task DeleteTransaction_Unauthorized_Returns401()
    {
        var response = await _client.DeleteAsync($"/bank-transactions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Import ────────────────────────────────────────────────

    [Fact]
    public async Task ImportHistoricalData_Unauthorized_Returns401()
    {
        var response = await _client.PostAsync("/bank-transactions/import", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Export ───────────────────────────────────────────────

    [Fact]
    public async Task ExportExcel_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/上海銀行/export?year=2026&month=3");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportExcel_ReturnsXlsx()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行/export?year=2026&month=3", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    // ── Import — TODO 2 ─────────────────────────────────────

    /// <summary>
    /// 匯入歷史資料（成功情境）。
    /// InMemory 資料庫不支援 ExecuteDeleteAsync（EF Core 批次刪除），
    /// 因此此測試需要真實資料庫（如 PostgreSQL）才能完整執行。
    /// </summary>
    [Fact(Skip = "InMemory 資料庫不支援 ExecuteDeleteAsync，需要真實資料庫才能測試匯入功能")]
    public async Task ImportHistoricalData_Success_ReturnsImportResult()
    {
        var token = await LoginAndGetTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, "/bank-transactions/import");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetProperty("totalTransactions").GetInt32().Should().BeGreaterThan(0);
    }

    // ── Export Validation — TODO 3 ──────────────────────────

    /// <summary>匯出 Excel 包含正確的欄位標題</summary>
    [Fact]
    public async Task ExportExcel_WithData_ContainsCorrectHeaders()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "標題驗證銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2027-01-15",
            description = "匯出標題測試",
            amount = 1000,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/export?year=2027&month=1", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        var expectedHeaders = new[] { "日期", "摘要", "歸屬部門", "收入", "支出", "手續費", "餘額", "已回收", "已寄送" };
        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            ws.Cell(5, i + 1).GetString().Should().Be(expectedHeaders[i],
                because: $"欄位 {i + 1} 標題應為「{expectedHeaders[i]}」");
        }
    }

    /// <summary>匯出 Excel 資料列與交易內容一致</summary>
    [Fact]
    public async Task ExportExcel_WithData_ContainsTransactionRows()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "資料驗證銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2027-02-01",
            description = "匯出資料測試收入",
            amount = 8000,
            fee = 10,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2027-02-15",
            description = "匯出資料測試支出",
            amount = 3000,
            fee = 20,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/export?year=2027&month=2", token));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // Row 6: 第一筆交易（收入）—— 日期使用 ToString("yyyy/MM/dd") 的格式
        ws.Cell(6, 1).GetString().Should().Be(new DateOnly(2027, 2, 1).ToString("yyyy/MM/dd"));
        ws.Cell(6, 2).GetString().Should().Be("匯出資料測試收入");
        ws.Cell(6, 4).GetDouble().Should().Be(8000);
        ws.Cell(6, 5).GetDouble().Should().Be(0);
        ws.Cell(6, 6).GetDouble().Should().Be(10);

        // Row 7: 第二筆交易（支出）
        ws.Cell(7, 1).GetString().Should().Be(new DateOnly(2027, 2, 15).ToString("yyyy/MM/dd"));
        ws.Cell(7, 2).GetString().Should().Be("匯出資料測試支出");
        ws.Cell(7, 4).GetDouble().Should().Be(0);
        ws.Cell(7, 5).GetDouble().Should().Be(3000);
        ws.Cell(7, 6).GetDouble().Should().Be(20);
    }

    /// <summary>匯出 Excel 摘要區段包含正確的期初與期末餘額</summary>
    [Fact]
    public async Task ExportExcel_WithData_VerifySummarySection()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "摘要驗證銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2027-03-01",
            description = "摘要測試收入",
            amount = 10000,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2027-03-15",
            description = "摘要測試支出",
            amount = 4000,
            fee = 50,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/export?year=2027&month=3", token));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // Row 3: 摘要區段
        ws.Cell(3, 1).GetString().Should().Be("期初餘額");
        ws.Cell(3, 2).GetDouble().Should().Be(0, because: "無歷史交易的銀行期初餘額為 0");
        ws.Cell(3, 3).GetString().Should().Be("期間收入");
        ws.Cell(3, 4).GetDouble().Should().Be(10000);
        ws.Cell(3, 5).GetString().Should().Be("期間支出");
        ws.Cell(3, 6).GetDouble().Should().Be(4050, because: "TotalExpense = 4000 + 50(手續費) = 4050");
        ws.Cell(3, 7).GetString().Should().Be("期末餘額");
        ws.Cell(3, 8).GetDouble().Should().Be(5950, because: "ClosingBalance = 0 + 10000 - 4050 = 5950");
    }

    /// <summary>
    /// 匯出 Excel 合計列的手續費計算驗證。
    /// 確認合計列的「支出」欄正確排除手續費，而「手續費」欄獨立加總。
    /// TotalExpense 已包含所有手續費（含收入交易的手續費），
    /// 合計列支出 = TotalExpense - Sum(Fee)，結果等於純支出金額，邏輯正確。
    /// </summary>
    [Fact]
    public async Task ExportExcel_VerifyTotalsRow_FeesCalculation()
    {
        var token = await LoginAndGetTokenAsync();
        var bank = "合計驗證銀行";

        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 0,
            transactionDate = "2027-04-01",
            description = "手續費測試收入",
            amount = 5000,
            fee = 10,
        }));
        await _client.SendAsync(AuthPost($"/bank-transactions/{bank}", token, new
        {
            transactionType = 1,
            transactionDate = "2027-04-15",
            description = "手續費測試支出",
            amount = 2000,
            fee = 30,
        }));

        var response = await _client.SendAsync(
            AuthGet($"/bank-transactions/{bank}/export?year=2027&month=4", token));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        // 合計列在 row 8（6 + 2 筆交易）
        var totalRow = 8;
        ws.Cell(totalRow, 1).GetString().Should().Be("合計");
        ws.Cell(totalRow, 4).GetDouble().Should().Be(5000, because: "收入合計");
        // TotalExpense = 2000(支出) + 40(全部手續費) = 2040
        // 合計列支出 = TotalExpense - allFees = 2040 - 40 = 2000（純支出金額）
        ws.Cell(totalRow, 5).GetDouble().Should().Be(2000, because: "支出合計（不含手續費）");
        ws.Cell(totalRow, 6).GetDouble().Should().Be(40, because: "手續費合計 = 10 + 30");
    }

    // ── CRUD Validation — TODO 4 ────────────────────────────

    /// <summary>
    /// 建立交易 — 負數金額。
    /// Amount 有 [Range(0.01, double.MaxValue)] 驗證，負值應被拒絕。
    /// </summary>
    [Fact]
    public async Task CreateTransaction_NegativeAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2027-05-01",
            description = "負數金額測試",
            amount = -100,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 建立交易 — 負數手續費。
    /// Fee 有 [Range(0, double.MaxValue)] 驗證，負值應被拒絕。
    /// </summary>
    [Fact]
    public async Task CreateTransaction_NegativeFee_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2027-05-01",
            description = "負數手續費測試",
            amount = 100,
            fee = -50,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 建立交易 — 金額為零。
    /// Amount 有 [Range(0.01, double.MaxValue)] 驗證，最小值為 0.01，因此 0 不被接受。
    /// </summary>
    [Fact]
    public async Task CreateTransaction_ZeroAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2027-05-01",
            description = "零金額測試",
            amount = 0,
        }));

        // [Range(0.01, double.MaxValue)] 不允許 0
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 建立交易 — 摘要僅含空白字元。
    /// Description 有 [Required] 驗證，ASP.NET Core 預設將全空白字串視為無效值（Trim 後為空）。
    /// </summary>
    [Fact]
    public async Task CreateTransaction_WhitespaceOnlyDescription_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2027-05-01",
            description = "   ",
            amount = 100,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 查詢交易 — 無效的月份 0。
    /// DateOnly 建構子不接受月份 0，會拋出 ArgumentOutOfRangeException。
    /// </summary>
    [Fact]
    public async Task GetTransactions_InvalidMonth0_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2027&month=0", token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// 查詢交易 — 無效的月份 13。
    /// DateOnly 建構子不接受月份 13，會拋出 ArgumentOutOfRangeException。
    /// </summary>
    [Fact]
    public async Task GetTransactions_InvalidMonth13_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2027&month=13", token));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>更新交易 — 從收入改為支出，驗證類型轉換成功</summary>
    [Fact]
    public async Task UpdateTransaction_IncomeToExpense_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        // 建立收入交易
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2027-06-01",
            description = "類型轉換測試",
            amount = 5000,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // 更新為支出
        var updateResponse = await _client.SendAsync(AuthPut($"/bank-transactions/{id}", token, new
        {
            transactionType = 1,
            transactionDate = "2027-06-01",
            description = "類型轉換測試（已改為支出）",
            amount = 5000,
        }));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證更新後的交易類型
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2027&month=6", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = getBody.GetProperty("data");
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var updated = transactions.EnumerateArray()
            .First(t => t.GetProperty("id").GetString() == id);
        updated.GetProperty("transactionType").GetInt32().Should().Be(1);
        updated.GetProperty("description").GetString().Should().Be("類型轉換測試（已改為支出）");
    }

    // ── ReceiptCollected Field ──────────────────────────────

    /// <summary>建立支出交易時帶 receiptCollected=true，回傳結果應包含該欄位</summary>
    [Fact]
    public async Task Create_WithReceiptCollected_ReturnsCreatedTransaction()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2028-01-15",
            description = "收據已領取測試",
            amount = 5000,
            receiptCollected = true,
            receiptMailed = false,
        }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("receiptCollected").GetBoolean().Should().BeTrue();
        data.GetProperty("receiptMailed").GetBoolean().Should().BeFalse();
    }

    /// <summary>更新支出交易的 receiptCollected 欄位，驗證 GET 回傳更新後的值</summary>
    [Fact]
    public async Task Update_ReceiptCollectedField_UpdatesSuccessfully()
    {
        var token = await LoginAndGetTokenAsync();

        // 建立支出交易（receiptCollected=false）
        var createResponse = await _client.SendAsync(AuthPost("/bank-transactions/合作金庫", token, new
        {
            transactionType = 1,
            transactionDate = "2028-02-01",
            description = "收據領取更新測試",
            amount = 3000,
            receiptCollected = false,
            receiptMailed = false,
        }));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = createBody.GetProperty("data").GetProperty("id").GetString();

        // 更新 receiptCollected=true
        var updateResponse = await _client.SendAsync(AuthPut($"/bank-transactions/{id}", token, new
        {
            transactionType = 1,
            transactionDate = "2028-02-01",
            description = "收據領取更新測試",
            amount = 3000,
            receiptCollected = true,
            receiptMailed = false,
        }));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證 GET 回傳更新後的值
        var getResponse = await _client.SendAsync(
            AuthGet("/bank-transactions/合作金庫?year=2028&month=2", token));
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var transactions = getBody.GetProperty("data");

        var updated = transactions.EnumerateArray()
            .First(t => t.GetProperty("id").GetString() == id);
        updated.GetProperty("receiptCollected").GetBoolean().Should().BeTrue();
    }

    // ── GET Receipt Tracking ────────────────────────────────

    /// <summary>未登入查詢收據追蹤應回傳 401</summary>
    [Fact]
    public async Task GetReceiptTracking_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/bank-transactions/receipt-tracking?year=2029");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>查詢年度未處理收據，應回傳支出交易中尚未處理的收據</summary>
    [Fact]
    public async Task GetReceiptTracking_WithYear_ReturnsUnprocessedReceipts()
    {
        var token = await LoginAndGetTokenAsync();

        // 建立支出交易（收據未領取）
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2029-01-10",
            description = "收據追蹤測試-支出未領取",
            amount = 1000,
            receiptCollected = false,
            receiptMailed = false,
        }));

        // 建立收入交易（不應出現在收據追蹤）
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 0,
            transactionDate = "2029-01-15",
            description = "收據追蹤測試-收入",
            amount = 2000,
        }));

        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/receipt-tracking?year=2029", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        data.GetProperty("notCollectedCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        var items = data.GetProperty("items");
        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "收據追蹤測試-支出未領取")
            .Should().BeTrue();
        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "收據追蹤測試-收入")
            .Should().BeFalse();
    }

    /// <summary>指定年月篩選，只回傳該月的未處理收據</summary>
    [Fact]
    public async Task GetReceiptTracking_WithYearAndMonth_FiltersCorrectly()
    {
        var token = await LoginAndGetTokenAsync();

        // 三月的支出交易
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2029-03-10",
            description = "收據追蹤-三月",
            amount = 1000,
            receiptCollected = false,
            receiptMailed = false,
        }));

        // 四月的支出交易
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2029-04-10",
            description = "收據追蹤-四月",
            amount = 2000,
            receiptCollected = false,
            receiptMailed = false,
        }));

        // 查詢三月
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/receipt-tracking?year=2029&month=3", token));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = body.GetProperty("data").GetProperty("items");

        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "收據追蹤-三月")
            .Should().BeTrue();
        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "收據追蹤-四月")
            .Should().BeFalse();
    }

    /// <summary>無未處理收據時回傳空清單</summary>
    [Fact]
    public async Task GetReceiptTracking_NoUnprocessedReceipts_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();

        // 查詢一個不太會有資料的年月
        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/receipt-tracking?year=2029&month=12", token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = body.GetProperty("data");
        data.GetProperty("totalCount").GetInt32().Should().Be(0);
        data.GetProperty("notCollectedCount").GetInt32().Should().Be(0);
        data.GetProperty("notMailedCount").GetInt32().Should().Be(0);
        data.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    /// <summary>不同銀行的未處理收據都應被回傳</summary>
    [Fact]
    public async Task GetReceiptTracking_MixedBanks_ReturnsBothBankResults()
    {
        var token = await LoginAndGetTokenAsync();

        // 上海銀行的支出未處理收據
        await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2029-05-01",
            description = "混合銀行測試-上海",
            amount = 1000,
            receiptCollected = false,
            receiptMailed = false,
        }));

        // 合作金庫的支出未處理收據
        await _client.SendAsync(AuthPost("/bank-transactions/合作金庫", token, new
        {
            transactionType = 1,
            transactionDate = "2029-05-15",
            description = "混合銀行測試-合庫",
            amount = 2000,
            receiptCollected = true,
            receiptMailed = false,
        }));

        var response = await _client.SendAsync(
            AuthGet("/bank-transactions/receipt-tracking?year=2029&month=5", token));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = body.GetProperty("data").GetProperty("items");

        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "混合銀行測試-上海")
            .Should().BeTrue();
        items.EnumerateArray()
            .Any(i => i.GetProperty("description").GetString() == "混合銀行測試-合庫")
            .Should().BeTrue();
    }

    // ── Batch Update Department ─────────────────────────────

    [Fact]
    public async Task BatchUpdateDepartment_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/bank-transactions/batch-department", new
        {
            ids = new[] { Guid.NewGuid() },
            departmentId = (Guid?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BatchUpdateDepartment_AssignDepartment_UpdatesAll()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await CreateDepartmentAsync(token, "批次部門測試");
        var tx1Id = await CreateTransactionAsync(token, "合作金庫", new
        {
            transactionType = 1,
            transactionDate = "2030-01-10",
            description = "批次測試支出一",
            amount = 100,
        });
        var tx2Id = await CreateTransactionAsync(token, "合作金庫", new
        {
            transactionType = 1,
            transactionDate = "2030-01-11",
            description = "批次測試支出二",
            amount = 200,
        });

        var response = await _client.SendAsync(
            AuthPost("/bank-transactions/batch-department", token, new
            {
                ids = new[] { tx1Id, tx2Id },
                departmentId = deptId,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var listResp = await _client.SendAsync(
            AuthGet("/bank-transactions/合作金庫?year=2030&month=1", token));
        var txArray = GetData(await ReadBodyAsync(listResp)).EnumerateArray().ToList();

        txArray.Where(t => t.GetProperty("departmentName").GetString() == "批次部門測試")
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task BatchUpdateDepartment_ClearDepartment_SetsNull()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await CreateDepartmentAsync(token, "清除測試部門");
        var txId = await CreateTransactionAsync(token, "合作金庫", new
        {
            transactionType = 1,
            transactionDate = "2030-02-10",
            description = "清除部門測試",
            amount = 300,
            departmentId = deptId,
        });

        var response = await _client.SendAsync(
            AuthPost("/bank-transactions/batch-department", token, new
            {
                ids = new[] { txId },
                departmentId = (string?)null,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResp = await _client.SendAsync(
            AuthGet("/bank-transactions/合作金庫?year=2030&month=2", token));
        var tx = GetData(await ReadBodyAsync(listResp)).EnumerateArray()
            .First(t => t.GetProperty("description").GetString() == "清除部門測試");

        tx.GetProperty("departmentId").ValueKind.Should().Be(JsonValueKind.Null);
        tx.GetProperty("departmentName").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task BatchUpdateDepartment_EmptyIds_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await _client.SendAsync(
            AuthPost("/bank-transactions/batch-department", token, new
            {
                ids = Array.Empty<Guid>(),
                departmentId = (Guid?)null,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BatchUpdateDepartment_NonExistentTransactionId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await _client.SendAsync(
            AuthPost("/bank-transactions/batch-department", token, new
            {
                ids = new[] { Guid.NewGuid() },
                departmentId = (Guid?)null,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static HttpRequestMessage AuthPatch(string url, string token, object data)
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(data),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // ── PATCH Receipt Status ──────────────────────────────────

    /// <summary>未登入呼叫 PATCH receipt-status 應回傳 401</summary>
    [Fact]
    public async Task PatchReceiptStatus_Unauthorized_Returns401()
    {
        var req = new HttpRequestMessage(HttpMethod.Patch, $"/bank-transactions/{Guid.NewGuid()}/receipt-status")
        {
            Content = JsonContent.Create(new { receiptCollected = true }),
        };
        var response = await _client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>對不存在的交易 PATCH receipt-status 應回傳 404</summary>
    [Fact]
    public async Task PatchReceiptStatus_NotFound_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await _client.SendAsync(AuthPatch(
            $"/bank-transactions/{Guid.NewGuid()}/receipt-status",
            token,
            new { receiptCollected = true }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>PATCH receiptCollected=true 後，GET 應回傳更新後的值</summary>
    [Fact]
    public async Task PatchReceiptStatus_SetCollected_UpdatesField()
    {
        var token = await LoginAndGetTokenAsync();

        // 建立支出交易
        var createResp = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2031-01-10",
            description = "收據狀態 PATCH 測試",
            amount = 5000,
            receiptCollected = false,
            receiptMailed = false,
        }));
        var id = GetData(await ReadBodyAsync(createResp)).GetProperty("id").GetString()!;

        // PATCH — 標記已回收
        var patchResp = await _client.SendAsync(AuthPatch(
            $"/bank-transactions/{id}/receipt-status",
            token,
            new { receiptCollected = true }));

        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(patchResp);
        body.GetProperty("code").GetInt32().Should().Be(0);

        // 驗證 GET 反映更新
        var listResp = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2031&month=1", token));
        var tx = GetData(await ReadBodyAsync(listResp)).EnumerateArray()
            .First(t => t.GetProperty("id").GetString() == id);

        tx.GetProperty("receiptCollected").GetBoolean().Should().BeTrue();
        tx.GetProperty("receiptMailed").GetBoolean().Should().BeFalse();
    }

    /// <summary>PATCH 只更新指定欄位，未傳入的欄位保持不變</summary>
    [Fact]
    public async Task PatchReceiptStatus_PartialUpdate_OnlyChangesSpecifiedField()
    {
        var token = await LoginAndGetTokenAsync();

        // 建立已回收、未寄送的支出交易
        var createResp = await _client.SendAsync(AuthPost("/bank-transactions/上海銀行", token, new
        {
            transactionType = 1,
            transactionDate = "2031-02-10",
            description = "收據狀態部分更新測試",
            amount = 3000,
            receiptCollected = true,
            receiptMailed = false,
        }));
        var id = GetData(await ReadBodyAsync(createResp)).GetProperty("id").GetString()!;

        // PATCH — 只標記已寄送（不傳 receiptCollected）
        var patchResp = await _client.SendAsync(AuthPatch(
            $"/bank-transactions/{id}/receipt-status",
            token,
            new { receiptMailed = true }));

        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證兩個欄位都正確
        var listResp = await _client.SendAsync(
            AuthGet("/bank-transactions/上海銀行?year=2031&month=2", token));
        var tx = GetData(await ReadBodyAsync(listResp)).EnumerateArray()
            .First(t => t.GetProperty("id").GetString() == id);

        tx.GetProperty("receiptCollected").GetBoolean().Should().BeTrue(because: "未傳入的欄位應保持原值");
        tx.GetProperty("receiptMailed").GetBoolean().Should().BeTrue(because: "應更新為 true");
    }
}
