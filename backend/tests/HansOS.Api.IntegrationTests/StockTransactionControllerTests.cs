using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class StockTransactionControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── POST /finance/stocks/buy (Auth) ─────────────

    [Fact]
    public async Task Buy_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/finance/stocks/buy", new
        {
            stockSymbol = "2330",
            stockName = "台積電",
            shares = 1000,
            pricePerShare = 500,
            tradeDate = "2025-04-01",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /finance/stocks/buy (Commission) ───────

    [Fact]
    public async Task Buy_ValidData_CalculatesCommission()
    {
        var token = await LoginAndGetTokenAsync();

        // Total = 1000 * 500 = 500,000
        // Commission = max(floor(500000 * 0.001425 * 0.6), 20) = max(floor(427.5), 20) = 427
        var response = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2330",
            stockName = "台積電",
            shares = 1000,
            pricePerShare = 500,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("tradeType").GetString().Should().Be("Buy");
        data.GetProperty("totalAmount").GetDecimal().Should().Be(500_000);
        data.GetProperty("commission").GetDecimal().Should().Be(427);
        data.GetProperty("tax").GetDecimal().Should().Be(0);
        data.GetProperty("netAmount").GetDecimal().Should().Be(500_000 + 427);
    }

    [Fact]
    public async Task Buy_SmallAmount_MinimumCommission()
    {
        var token = await LoginAndGetTokenAsync();

        // Total = 1 * 100 = 100
        // Commission = max(floor(100 * 0.001425 * 0.6), 20) = max(floor(0.0855), 20) = max(0, 20) = 20
        var response = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "9999",
            stockName = "小額測試股",
            shares = 1,
            pricePerShare = 100,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetProperty("commission").GetDecimal().Should().Be(20);
    }

    // ── POST /finance/stocks/sell ───────────────────

    [Fact]
    public async Task Sell_ValidData_CalculatesTaxAndCommission()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy first
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2317",
            stockName = "鴻海",
            shares = 1000,
            pricePerShare = 100,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });

        // Sell: Total = 1000 * 120 = 120,000
        // Commission = max(floor(120000 * 0.001425 * 0.6), 20) = max(floor(102.6), 20) = 102
        // Tax (non-ETF) = floor(120000 * 0.003) = 360
        var response = await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "2317",
            shares = 1000,
            pricePerShare = 120,
            commissionDiscount = 0.6,
            isEtf = false,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("tradeType").GetString().Should().Be("Sell");
        data.GetProperty("commission").GetDecimal().Should().Be(102);
        data.GetProperty("tax").GetDecimal().Should().Be(360);
    }

    [Fact]
    public async Task Sell_Etf_CalculatesReducedTax()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy ETF
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "0050",
            stockName = "元大台灣50",
            shares = 1000,
            pricePerShare = 150,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });

        // Sell ETF: Total = 1000 * 160 = 160,000
        // Tax (ETF) = floor(160000 * 0.001) = 160
        var response = await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "0050",
            shares = 1000,
            pricePerShare = 160,
            commissionDiscount = 0.6,
            isEtf = true,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetProperty("tax").GetDecimal().Should().Be(160);
    }

    [Fact]
    public async Task Sell_ExceedHoldings_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy 100 shares
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "3008",
            stockName = "大立光",
            shares = 100,
            pricePerShare = 2000,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });

        // Try to sell 200 shares
        var response = await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "3008",
            shares = 200,
            pricePerShare = 2100,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /finance/stocks/holdings ────────────────

    [Fact]
    public async Task GetHoldings_AfterBuySell_CalculatesCorrectly()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy 100 @ 50 → Total = 5000
        var buy1Resp = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "6505",
            stockName = "台塑化",
            shares = 100,
            pricePerShare = 50,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });
        var buy1Body = await ReadBodyAsync(buy1Resp);
        var comm1 = buy1Body.GetProperty("data").GetProperty("commission").GetDecimal();

        // Buy 200 @ 60 → Total = 12000
        var buy2Resp = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "6505",
            stockName = "台塑化",
            shares = 200,
            pricePerShare = 60,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-15",
        });
        var buy2Body = await ReadBodyAsync(buy2Resp);
        var comm2 = buy2Body.GetProperty("data").GetProperty("commission").GetDecimal();

        var response = await AuthorizedGetAsync("/finance/stocks/holdings", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var holdings = body.GetProperty("data");
        holdings.ValueKind.Should().Be(JsonValueKind.Array);

        // Find the 6505 holding
        var found = false;
        foreach (var h in holdings.EnumerateArray())
        {
            if (h.GetProperty("stockSymbol").GetString() == "6505")
            {
                h.GetProperty("shares").GetInt32().Should().Be(300);
                // TotalCost = (5000 + comm1) + (12000 + comm2)
                var expectedTotalCost = 5000 + comm1 + 12000 + comm2;
                h.GetProperty("totalCost").GetDecimal().Should().Be(expectedTotalCost);
                // AvgCost = TotalCost / 300
                var expectedAvgCost = Math.Round(expectedTotalCost / 300, 4);
                h.GetProperty("averageCost").GetDecimal().Should().Be(expectedAvgCost);
                found = true;
            }
        }

        found.Should().BeTrue("should have a holding for symbol 6505");
    }

    [Fact]
    public async Task GetHoldings_AllSold_ReturnsEmpty()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy 100 shares
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "1301",
            stockName = "台塑",
            shares = 100,
            pricePerShare = 80,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });

        // Sell all 100 shares
        await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "1301",
            shares = 100,
            pricePerShare = 90,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        var response = await AuthorizedGetAsync("/finance/stocks/holdings", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var holdings = body.GetProperty("data");

        // 1301 should not appear (all sold)
        foreach (var h in holdings.EnumerateArray())
        {
            h.GetProperty("stockSymbol").GetString().Should().NotBe("1301");
        }
    }

    // ── Realized P/L ────────────────────────────────

    [Fact]
    public async Task RealizedPL_Calculation()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy 100 @ 50 → cost = 5000 + commission
        var buy1Resp = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2454",
            stockName = "聯發科",
            shares = 100,
            pricePerShare = 50,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });
        var buy1Body = await ReadBodyAsync(buy1Resp);
        var comm1 = buy1Body.GetProperty("data").GetProperty("commission").GetDecimal();

        // Buy 200 @ 60 → cost = 12000 + commission
        var buy2Resp = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2454",
            stockName = "聯發科",
            shares = 200,
            pricePerShare = 60,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-15",
        });
        var buy2Body = await ReadBodyAsync(buy2Resp);
        var comm2 = buy2Body.GetProperty("data").GetProperty("commission").GetDecimal();

        // Sell 100 @ 70
        var sellResp = await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "2454",
            shares = 100,
            pricePerShare = 70,
            commissionDiscount = 0.6,
            isEtf = false,
            tradeDate = "2025-04-01",
        });

        var sellBody = await ReadBodyAsync(sellResp);
        sellBody.GetProperty("code").GetInt32().Should().Be(0);

        var sellData = sellBody.GetProperty("data");
        var sellCommission = sellData.GetProperty("commission").GetDecimal();
        var sellTax = sellData.GetProperty("tax").GetDecimal();

        // AvgCost = (5000 + comm1 + 12000 + comm2) / 300
        var totalCost = 5000m + comm1 + 12000m + comm2;
        var avgCost = totalCost / 300m;
        var costOfSold = avgCost * 100m;

        // NetProceeds = 7000 - sellCommission - sellTax
        var netProceeds = 7000m - sellCommission - sellTax;
        var expectedPL = netProceeds - costOfSold;

        // Verify the realized P/L is populated and approximately correct
        sellData.TryGetProperty("realizedProfitLoss", out var plProp).Should().BeTrue();
        plProp.ValueKind.Should().NotBe(JsonValueKind.Null);
        var actualPL = plProp.GetDecimal();
        actualPL.Should().BeApproximately(expectedPL, 1m);
    }

    // ── GET /finance/stocks/summary ─────────────────

    [Fact]
    public async Task GetProfitSummary_ReturnsCorrectTotals()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2881",
            stockName = "富邦金",
            shares = 1000,
            pricePerShare = 60,
            commissionDiscount = 0.6,
            tradeDate = "2025-03-01",
        });

        // Sell
        await AuthorizedPostAsync("/finance/stocks/sell", token, new
        {
            stockSymbol = "2881",
            shares = 500,
            pricePerShare = 70,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        var response = await AuthorizedGetAsync("/finance/stocks/summary", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("totalCommission").GetDecimal().Should().BeGreaterThan(0);
        data.GetProperty("totalTax").GetDecimal().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("transactionCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetProfitSummary_WithYearFilter()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy in 2024
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2882",
            stockName = "國泰金",
            shares = 100,
            pricePerShare = 50,
            commissionDiscount = 0.6,
            tradeDate = "2024-06-01",
        });

        // Buy in 2025
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2883",
            stockName = "開發金",
            shares = 100,
            pricePerShare = 30,
            commissionDiscount = 0.6,
            tradeDate = "2025-06-01",
        });

        // Filter by 2025 only
        var response = await AuthorizedGetAsync("/finance/stocks/summary?year=2025", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("transactionCount").GetInt32().Should().BeGreaterThanOrEqualTo(1);
    }

    // ── GET /finance/stocks/transactions ────────────

    [Fact]
    public async Task GetTransactions_FilterBySymbol()
    {
        var token = await LoginAndGetTokenAsync();

        // Buy two different stocks
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2330",
            stockName = "台積電",
            shares = 10,
            pricePerShare = 600,
            commissionDiscount = 0.6,
            tradeDate = "2025-05-01",
        });
        await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "0056",
            stockName = "元大高股息",
            shares = 100,
            pricePerShare = 35,
            commissionDiscount = 0.6,
            tradeDate = "2025-05-01",
        });

        // Filter by 2330
        var response = await AuthorizedGetAsync("/finance/stocks/transactions?symbol=2330", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);

        // All returned transactions should be for 2330
        foreach (var txn in data.EnumerateArray())
        {
            txn.GetProperty("stockSymbol").GetString().Should().Be("2330");
        }
    }

    // ── DELETE /finance/stocks/transactions/{id} ────

    [Fact]
    public async Task DeleteTransaction_Succeeds()
    {
        var token = await LoginAndGetTokenAsync();

        var createResp = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "5880",
            stockName = "合庫金",
            shares = 100,
            pricePerShare = 25,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });
        var createBody = await ReadBodyAsync(createResp);
        var id = createBody.GetProperty("data").GetProperty("id").GetString()!;

        var deleteResp = await AuthorizedDeleteAsync($"/finance/stocks/transactions/{id}", token);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's gone from the transaction list
        var listResp = await AuthorizedGetAsync("/finance/stocks/transactions?symbol=5880", token);
        var listBody = await ReadBodyAsync(listResp);
        var data = listBody.GetProperty("data");

        foreach (var txn in data.EnumerateArray())
        {
            txn.GetProperty("id").GetString().Should().NotBe(id);
        }
    }

    // ── Validation ──────────────────────────────────

    [Fact]
    public async Task Buy_ZeroShares_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/finance/stocks/buy", token, new
        {
            stockSymbol = "2330",
            stockName = "台積電",
            shares = 0,
            pricePerShare = 500,
            commissionDiscount = 0.6,
            tradeDate = "2025-04-01",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ─────────────────────────────────────

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
        => _client.SendAsync(AuthGet(url, token));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(AuthPost(url, token, data));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(AuthPut(url, token, data));

    private Task<HttpResponseMessage> AuthorizedDeleteAsync(string url, string token)
        => _client.SendAsync(CreateAuthorizedRequest(HttpMethod.Delete, url, token));

    private static HttpRequestMessage AuthGet(string url, string token)
        => CreateAuthorizedRequest(HttpMethod.Get, url, token);

    private static HttpRequestMessage AuthPost(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data));

    private static HttpRequestMessage AuthPut(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data));

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string token,
        HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
