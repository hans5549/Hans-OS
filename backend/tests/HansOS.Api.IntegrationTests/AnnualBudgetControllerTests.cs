using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class AnnualBudgetControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── GetOverview ─────────────────────────────────

    [Fact]
    public async Task GetOverview_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/annual-budgets/2026");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOverview_ValidYear_AutoInitializesAndReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/annual-budgets/2099", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("year").GetInt32().Should().Be(2099);
        data.GetProperty("status").GetString().Should().Be("Draft");
        data.GetProperty("departments").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetOverview_SameYear_ReturnsSameBudget()
    {
        var token = await LoginAndGetTokenAsync();

        var resp1 = await AuthorizedGetAsync("/annual-budgets/2098", token);
        var body1 = await ReadBodyAsync(resp1);
        var id1 = body1.GetProperty("data").GetProperty("id").GetString();

        var resp2 = await AuthorizedGetAsync("/annual-budgets/2098", token);
        var body2 = await ReadBodyAsync(resp2);
        var id2 = body2.GetProperty("data").GetProperty("id").GetString();

        id1.Should().Be(id2);
    }

    [Fact]
    public async Task GetOverview_WithDepartments_ShowsDepartmentSummaries()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算測試部門A");
        var response = await AuthorizedGetAsync("/annual-budgets/2097", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");
        departments.GetArrayLength().Should().BeGreaterThan(0);

        var found = false;
        foreach (var dept in departments.EnumerateArray())
        {
            if (dept.GetProperty("departmentId").GetString() == deptId.ToString())
            {
                found = true;
                dept.GetProperty("budgetAmount").GetDecimal().Should().Be(0);
                dept.GetProperty("itemCount").GetInt32().Should().Be(0);
            }
        }

        found.Should().BeTrue("部門應出現在年度預算中");
    }

    // ── UpdateStatus ────────────────────────────────

    [Fact]
    public async Task UpdateStatus_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/annual-budgets/2026/status", new
        {
            status = "Submitted",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2096, token);
        var response = await AuthorizedPutAsync("/annual-budgets/2096/status", token, new { status = "Submitted" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resp2 = await AuthorizedGetAsync("/annual-budgets/2096", token);
        var body = await ReadBodyAsync(resp2);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2095, token);

        var response = await AuthorizedPutAsync("/annual-budgets/2095/status", token, new { status = "InvalidStatus" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentYear_Returns404()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPutAsync("/annual-budgets/2050/status", token, new { status = "Draft" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_DraftToApproved_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2085, token);

        // Draft → Approved 自由切換
        var response = await AuthorizedPutAsync("/annual-budgets/2085/status", token, new { status = "Approved" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOverview_YearOutOfRange_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var response = await AuthorizedGetAsync("/annual-budgets/1999", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetDepartmentItems ──────────────────────────

    [Fact]
    public async Task GetDepartmentItems_Unauthorized_Returns401()
    {
        var deptId = Guid.NewGuid();
        var response = await _client.GetAsync($"/annual-budgets/2026/departments/{deptId}/items");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDepartmentItems_EmptyDepartment_ReturnsEmptyList()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算空部門");
        await InitializeAnnualBudgetAsync(2094, token);
        var response = await AuthorizedGetAsync(DepartmentItemsUrl(2094, deptId), token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    // ── SaveDepartmentItems ─────────────────────────

    [Fact]
    public async Task SaveDepartmentItems_Unauthorized_Returns401()
    {
        var deptId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync(
            $"/annual-budgets/2026/departments/{deptId}/items",
            new { items = Array.Empty<object>() });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SaveDepartmentItems_ValidItems_ReturnsCreatedItems()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算儲存部門");
        await InitializeAnnualBudgetAsync(2093, token);

        var response = await SaveDepartmentItemsAsync(
            2093,
            deptId,
            token,
            BudgetItemPayload(1, "全國排球賽", "場地費", 50000m, note: "中央大學體育館"),
            BudgetItemPayload(2, "全國排球賽", "裁判費", 14000m, note: "20人×700元"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
        data[0].GetProperty("activityName").GetString().Should().Be("全國排球賽");
        data[0].GetProperty("contentItem").GetString().Should().Be("場地費");
        data[0].GetProperty("amount").GetDecimal().Should().Be(50000m);
        data[1].GetProperty("contentItem").GetString().Should().Be("裁判費");
    }

    [Fact]
    public async Task SaveDepartmentItems_UpdateExisting_ModifiesItems()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算更新部門");
        await InitializeAnnualBudgetAsync(2092, token);

        var createResp = await SaveDepartmentItemsAsync(
            2092,
            deptId,
            token,
            BudgetItemPayload(1, "原始活動", "原始項目", 1000m));
        var createBody = await ReadBodyAsync(createResp);
        var itemId = createBody.GetProperty("data")[0].GetProperty("id").GetString();

        var updateResp = await SaveDepartmentItemsAsync(
            2092,
            deptId,
            token,
            BudgetItemPayload(
                1,
                "已更新活動",
                "已更新項目",
                2000m,
                id: itemId,
                actualAmount: 1800m,
                actualNote: "實際支出"));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateBody = await ReadBodyAsync(updateResp);
        var updated = updateBody.GetProperty("data")[0];
        updated.GetProperty("activityName").GetString().Should().Be("已更新活動");
        updated.GetProperty("amount").GetDecimal().Should().Be(2000m);
        updated.GetProperty("actualAmount").GetDecimal().Should().Be(1800m);
    }

    [Fact]
    public async Task SaveDepartmentItems_RemoveItems_DeletesOmitted()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算刪除部門");
        await InitializeAnnualBudgetAsync(2091, token);
        await SaveDepartmentItemsAsync(
            2091,
            deptId,
            token,
            BudgetItemPayload(1, "A", "A1", 100m),
            BudgetItemPayload(2, "B", "B1", 200m));

        var resp = await SaveDepartmentItemsAsync(
            2091,
            deptId,
            token,
            BudgetItemPayload(1, "A", "A1", 100m));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(resp);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SaveDepartmentItems_EmptyList_ClearsAll()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算清空部門");
        await InitializeAnnualBudgetAsync(2090, token);
        await SaveDepartmentItemsAsync(
            2090,
            deptId,
            token,
            BudgetItemPayload(1, "X", "X1", 500m));

        var resp = await SaveDepartmentItemsAsync(2090, deptId, token);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(resp);
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetOverview_AfterSaveItems_ReflectsTotals()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預算合計部門");
        await InitializeAnnualBudgetAsync(2089, token);
        await SaveDepartmentItemsAsync(
            2089,
            deptId,
            token,
            BudgetItemPayload(1, "E1", "C1", 10000m),
            BudgetItemPayload(2, "E2", "C2", 20000m));

        var resp = await AuthorizedGetAsync("/annual-budgets/2089", token);
        var body = await ReadBodyAsync(resp);
        var data = body.GetProperty("data");

        data.GetProperty("totalBudget").GetDecimal().Should().BeGreaterThanOrEqualTo(30000m);
    }

    // ── UpdateGrantedBudget ─────────────────────────

    [Fact]
    public async Task UpdateGrantedBudget_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/annual-budgets/2026/granted-budget", new
        {
            grantedBudget = 100000m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateGrantedBudget_ValidAmount_ReturnsOverviewWithAllocations()
    {
        var token = await LoginAndGetTokenAsync();
        var deptA = await EnsureDepartmentAsync("核定預算A部門");
        var deptB = await EnsureDepartmentAsync("核定預算B部門");
        await InitializeAnnualBudgetAsync(2079, token);

        // 部門A: 需求 60,000
        await SaveDepartmentItemsAsync(2079, deptA, token,
            BudgetItemPayload(1, "活動A1", "項目A1", 40000m),
            BudgetItemPayload(2, "活動A2", "項目A2", 20000m));

        // 部門B: 需求 40,000
        await SaveDepartmentItemsAsync(2079, deptB, token,
            BudgetItemPayload(1, "活動B1", "項目B1", 40000m));

        // 設定核定總預算 80,000（需求總額至少 100,000）
        var response = await AuthorizedPutAsync("/annual-budgets/2079/granted-budget", token,
            new { grantedBudget = 80000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");

        data.GetProperty("grantedBudget").GetDecimal().Should().Be(80000m);

        // 驗證各部門核定預算按比例分配
        var departments = data.GetProperty("departments");
        decimal totalAllocated = 0m;
        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            var allocatedAmount = dept.GetProperty("allocatedAmount").GetDecimal();

            if (deptId == deptA.ToString())
            {
                // 60,000 / 100,000 * 80,000 = 48,000
                allocatedAmount.Should().Be(48000m);
            }
            else if (deptId == deptB.ToString())
            {
                // 40,000 / 100,000 * 80,000 = 32,000
                allocatedAmount.Should().Be(32000m);
            }

            totalAllocated += allocatedAmount;
        }

        // 所有部門核定預算合計應等於核定總預算
        totalAllocated.Should().Be(80000m);
    }

    [Fact]
    public async Task UpdateGrantedBudget_NegativeAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2078, token);

        var response = await AuthorizedPutAsync("/annual-budgets/2078/granted-budget", token,
            new { grantedBudget = -1m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateGrantedBudget_ZeroAmount_SetsAllAllocationsToZero()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("核定零預算部門");
        await InitializeAnnualBudgetAsync(2077, token);
        await SaveDepartmentItemsAsync(2077, deptId, token,
            BudgetItemPayload(1, "活動Z", "項目Z", 10000m));

        var response = await AuthorizedPutAsync("/annual-budgets/2077/granted-budget", token,
            new { grantedBudget = 0m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");
        foreach (var dept in departments.EnumerateArray())
        {
            dept.GetProperty("allocatedAmount").GetDecimal().Should().Be(0m);
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_NoItems_SetsAllAllocationsToZero()
    {
        var token = await LoginAndGetTokenAsync();
        await EnsureDepartmentAsync("核定空預算部門");
        await InitializeAnnualBudgetAsync(2076, token);

        var response = await AuthorizedPutAsync("/annual-budgets/2076/granted-budget", token,
            new { grantedBudget = 50000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");
        foreach (var dept in departments.EnumerateArray())
        {
            dept.GetProperty("allocatedAmount").GetDecimal().Should().Be(0m);
        }
    }

    [Fact]
    public async Task SaveDepartmentItems_AfterGrantedBudgetSet_RecalculatesAllocations()
    {
        var token = await LoginAndGetTokenAsync();
        var deptC = await EnsureDepartmentAsync("核定重算C部門");
        var deptD = await EnsureDepartmentAsync("核定重算D部門");
        await InitializeAnnualBudgetAsync(2075, token);

        // 先設定部門C需求 50,000
        await SaveDepartmentItemsAsync(2075, deptC, token,
            BudgetItemPayload(1, "活動C", "項目C", 50000m));

        // 設定核定總預算 100,000
        await AuthorizedPutAsync("/annual-budgets/2075/granted-budget", token,
            new { grantedBudget = 100000m });

        // 新增部門D需求 50,000 → 重算比例
        await SaveDepartmentItemsAsync(2075, deptD, token,
            BudgetItemPayload(1, "活動D", "項目D", 50000m));

        // 取得最新 overview 確認重算
        var resp = await AuthorizedGetAsync("/annual-budgets/2075", token);
        var body = await ReadBodyAsync(resp);
        var departments = body.GetProperty("data").GetProperty("departments");

        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            if (deptId == deptC.ToString() || deptId == deptD.ToString())
            {
                // 各 50,000 / 100,000 * 100,000 = 50,000
                dept.GetProperty("allocatedAmount").GetDecimal().Should().Be(50000m);
            }
        }
    }

    [Fact]
    public async Task GetOverview_WithGrantedBudget_ReturnsGrantedBudgetField()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2074, token);

        // 設定核定總預算
        await AuthorizedPutAsync("/annual-budgets/2074/granted-budget", token,
            new { grantedBudget = 500000m });

        var resp = await AuthorizedGetAsync("/annual-budgets/2074", token);
        var body = await ReadBodyAsync(resp);
        var data = body.GetProperty("data");

        data.GetProperty("grantedBudget").GetDecimal().Should().Be(500000m);
    }

    [Fact]
    public async Task GetOverview_WithoutGrantedBudget_ReturnsNullGrantedBudget()
    {
        var token = await LoginAndGetTokenAsync();
        var resp = await AuthorizedGetAsync("/annual-budgets/2073", token);
        var body = await ReadBodyAsync(resp);
        var data = body.GetProperty("data");

        data.GetProperty("grantedBudget").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ── TODO 6: Status Transitions ──────────────────

    [Fact]
    public async Task UpdateStatus_SubmittedToDraft_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2030, token);

        // Draft → Submitted
        var resp1 = await AuthorizedPutAsync("/annual-budgets/2030/status", token, new { status = "Submitted" });
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Submitted → Draft（允許退回）
        var resp2 = await AuthorizedPutAsync("/annual-budgets/2030/status", token, new { status = "Draft" });
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview = await AuthorizedGetAsync("/annual-budgets/2030", token);
        var body = await ReadBodyAsync(overview);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task UpdateStatus_SubmittedToApproved_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2031, token);

        await AuthorizedPutAsync("/annual-budgets/2031/status", token, new { status = "Submitted" });
        var response = await AuthorizedPutAsync("/annual-budgets/2031/status", token, new { status = "Approved" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await AuthorizedGetAsync("/annual-budgets/2031", token);
        var body = await ReadBodyAsync(overview);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Approved");
    }

    [Fact]
    public async Task UpdateStatus_ApprovedToSettled_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2032, token);

        // 完整生命週期：Draft → Submitted → Approved → Settled
        await AuthorizedPutAsync("/annual-budgets/2032/status", token, new { status = "Submitted" });
        await AuthorizedPutAsync("/annual-budgets/2032/status", token, new { status = "Approved" });
        var response = await AuthorizedPutAsync("/annual-budgets/2032/status", token, new { status = "Settled" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var overview = await AuthorizedGetAsync("/annual-budgets/2032", token);
        var body = await ReadBodyAsync(overview);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Settled");
    }

    [Fact]
    public async Task UpdateStatus_SettledToAny_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2033, token);

        // Draft → Submitted → Approved → Settled
        await AuthorizedPutAsync("/annual-budgets/2033/status", token, new { status = "Submitted" });
        await AuthorizedPutAsync("/annual-budgets/2033/status", token, new { status = "Approved" });
        await AuthorizedPutAsync("/annual-budgets/2033/status", token, new { status = "Settled" });

        // Settled → Draft 自由切換
        var response = await AuthorizedPutAsync("/annual-budgets/2033/status", token, new { status = "Draft" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_DraftToDraft_Returns200_NoOp()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2034, token);

        // Draft → Draft 同狀態，回傳成功但不更新
        var response = await AuthorizedPutAsync("/annual-budgets/2034/status", token, new { status = "Draft" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_EmptyStatus_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2035, token);

        var response = await AuthorizedPutAsync("/annual-budgets/2035/status", token, new { status = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_CaseInsensitive_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2036, token);

        // 小寫 "submitted" 應因 Enum.TryParse ignoreCase:true 而成功
        var response = await AuthorizedPutAsync("/annual-budgets/2036/status", token, new { status = "submitted" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var overview = await AuthorizedGetAsync("/annual-budgets/2036", token);
        var body = await ReadBodyAsync(overview);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Submitted");
    }

    [Fact]
    public async Task UpdateStatus_YearAbove2100_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPutAsync("/annual-budgets/2101/status", token, new { status = "Submitted" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── TODO 7: Budget Items Validation ─────────────

    [Fact]
    public async Task GetDepartmentItems_WithItems_ReturnsOrderedBySequence()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("排序測試部門");
        await InitializeAnnualBudgetAsync(2038, token);

        // 以 sequence 3, 1, 2 順序儲存
        await SaveDepartmentItemsAsync(2038, deptId, token,
            BudgetItemPayload(3, "活動C", "項目C", 300m),
            BudgetItemPayload(1, "活動A", "項目A", 100m),
            BudgetItemPayload(2, "活動B", "項目B", 200m));

        var response = await AuthorizedGetAsync(DepartmentItemsUrl(2038, deptId), token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(3);
        data[0].GetProperty("sequence").GetInt32().Should().Be(1);
        data[1].GetProperty("sequence").GetInt32().Should().Be(2);
        data[2].GetProperty("sequence").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task GetDepartmentItems_InvalidDepartment_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        await InitializeAnnualBudgetAsync(2039, token);

        var fakeDeptId = Guid.NewGuid();
        var response = await AuthorizedGetAsync(DepartmentItemsUrl(2039, fakeDeptId), token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveDepartmentItems_ActivityNameEmpty_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("空活動名部門");
        await InitializeAnnualBudgetAsync(2040, token);

        var response = await SaveDepartmentItemsAsync(2040, deptId, token,
            BudgetItemPayload(1, "", "項目", 1000m));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveDepartmentItems_ActivityNameExceeds200_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("超長活動名部門");
        await InitializeAnnualBudgetAsync(2041, token);

        var longName = new string('A', 201);
        var response = await SaveDepartmentItemsAsync(2041, deptId, token,
            BudgetItemPayload(1, longName, "項目", 1000m));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveDepartmentItems_ContentItemEmpty_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("空內容項部門");
        await InitializeAnnualBudgetAsync(2042, token);

        var response = await SaveDepartmentItemsAsync(2042, deptId, token,
            BudgetItemPayload(1, "活動", "", 1000m));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveDepartmentItems_NegativeAmount_Returns400()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("負金額部門");
        await InitializeAnnualBudgetAsync(2043, token);

        var response = await SaveDepartmentItemsAsync(2043, deptId, token,
            BudgetItemPayload(1, "活動", "項目", -100m));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveDepartmentItems_UpdateNonExistentItemId_Returns404()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("假ID部門");
        await InitializeAnnualBudgetAsync(2044, token);

        var fakeId = Guid.NewGuid().ToString();
        var response = await SaveDepartmentItemsAsync(2044, deptId, token,
            BudgetItemPayload(1, "活動", "項目", 1000m, id: fakeId));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveDepartmentItems_MixedCreateUpdateDelete_Returns200()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("混合操作部門");
        await InitializeAnnualBudgetAsync(2045, token);

        // 先建立 3 個項目
        var createResp = await SaveDepartmentItemsAsync(2045, deptId, token,
            BudgetItemPayload(1, "保留活動", "保留項目", 1000m),
            BudgetItemPayload(2, "刪除活動", "刪除項目", 2000m),
            BudgetItemPayload(3, "刪除活動2", "刪除項目2", 3000m));
        var createBody = await ReadBodyAsync(createResp);
        var keepId = createBody.GetProperty("data")[0].GetProperty("id").GetString();

        // 混合操作：保留第1個（含 ID）、新增1個（無 ID）、省略第2、3個（刪除）
        var updateResp = await SaveDepartmentItemsAsync(2045, deptId, token,
            BudgetItemPayload(1, "已更新活動", "已更新項目", 1500m, id: keepId),
            BudgetItemPayload(2, "新增活動", "新增項目", 500m));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(updateResp);
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
        data[0].GetProperty("activityName").GetString().Should().Be("已更新活動");
        data[0].GetProperty("amount").GetDecimal().Should().Be(1500m);
        data[1].GetProperty("activityName").GetString().Should().Be("新增活動");
    }

    // ── TODO 8: Allocation Edge Cases ───────────────

    [Fact]
    public async Task UpdateGrantedBudget_ExactlyEqualsTotalRequested_AllocatesExactly()
    {
        var token = await LoginAndGetTokenAsync();
        var deptA = await EnsureDepartmentAsync("精確分配A部門");
        var deptB = await EnsureDepartmentAsync("精確分配B部門");
        await InitializeAnnualBudgetAsync(2055, token);

        await SaveDepartmentItemsAsync(2055, deptA, token,
            BudgetItemPayload(1, "活動A", "項目A", 60000m));
        await SaveDepartmentItemsAsync(2055, deptB, token,
            BudgetItemPayload(1, "活動B", "項目B", 40000m));

        // 核定 = 需求總額 100,000
        var response = await AuthorizedPutAsync("/annual-budgets/2055/granted-budget", token,
            new { grantedBudget = 100000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");

        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            var allocated = dept.GetProperty("allocatedAmount").GetDecimal();

            if (deptId == deptA.ToString())
                allocated.Should().Be(60000m);
            else if (deptId == deptB.ToString())
                allocated.Should().Be(40000m);
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_GreaterThanTotalRequested_ProportionalOverallocation()
    {
        var token = await LoginAndGetTokenAsync();
        var deptA = await EnsureDepartmentAsync("超額分配A部門");
        var deptB = await EnsureDepartmentAsync("超額分配B部門");
        await InitializeAnnualBudgetAsync(2056, token);

        // 需求：A=30000, B=20000, 總計=50000
        await SaveDepartmentItemsAsync(2056, deptA, token,
            BudgetItemPayload(1, "活動A", "項目A", 30000m));
        await SaveDepartmentItemsAsync(2056, deptB, token,
            BudgetItemPayload(1, "活動B", "項目B", 20000m));

        // 核定 100,000 > 需求 50,000
        var response = await AuthorizedPutAsync("/annual-budgets/2056/granted-budget", token,
            new { grantedBudget = 100000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");

        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            var allocated = dept.GetProperty("allocatedAmount").GetDecimal();

            if (deptId == deptA.ToString())
                allocated.Should().Be(60000m); // 100000 * 30000 / 50000
            else if (deptId == deptB.ToString())
                allocated.Should().Be(40000m); // 100000 * 20000 / 50000
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_ThreeDepartments_CorrectProportions()
    {
        var token = await LoginAndGetTokenAsync();
        var deptA = await EnsureDepartmentAsync("三部門比例A");
        var deptB = await EnsureDepartmentAsync("三部門比例B");
        var deptC = await EnsureDepartmentAsync("三部門比例C");
        await InitializeAnnualBudgetAsync(2057, token);

        // A=50000, B=30000, C=20000 → 總計=100000
        await SaveDepartmentItemsAsync(2057, deptA, token,
            BudgetItemPayload(1, "活動A", "項目A", 50000m));
        await SaveDepartmentItemsAsync(2057, deptB, token,
            BudgetItemPayload(1, "活動B", "項目B", 30000m));
        await SaveDepartmentItemsAsync(2057, deptC, token,
            BudgetItemPayload(1, "活動C", "項目C", 20000m));

        // 核定 80,000
        var response = await AuthorizedPutAsync("/annual-budgets/2057/granted-budget", token,
            new { grantedBudget = 80000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");

        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            var allocated = dept.GetProperty("allocatedAmount").GetDecimal();

            if (deptId == deptA.ToString())
                allocated.Should().Be(40000m); // 80000 * 50000 / 100000
            else if (deptId == deptB.ToString())
                allocated.Should().Be(24000m); // 80000 * 30000 / 100000
            else if (deptId == deptC.ToString())
                allocated.Should().Be(16000m); // 80000 * 20000 / 100000
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_RoundingRemainder_GoesToLargestDept()
    {
        var token = await LoginAndGetTokenAsync();
        var deptA = await EnsureDepartmentAsync("捨入最大A部門");
        var deptB = await EnsureDepartmentAsync("捨入較小B部門");
        var deptC = await EnsureDepartmentAsync("捨入較小C部門");
        await InitializeAnnualBudgetAsync(2058, token);

        // A=4000（最大）, B=1000, C=1000 → 總計=6000
        await SaveDepartmentItemsAsync(2058, deptA, token,
            BudgetItemPayload(1, "活動A", "項目A", 4000m));
        await SaveDepartmentItemsAsync(2058, deptB, token,
            BudgetItemPayload(1, "活動B", "項目B", 1000m));
        await SaveDepartmentItemsAsync(2058, deptC, token,
            BudgetItemPayload(1, "活動C", "項目C", 1000m));

        // 核定 10000 → 各部門: 6666.67, 1666.67, 1666.67 = 10000.01 → 差額 -0.01 歸最大部門
        var response = await AuthorizedPutAsync("/annual-budgets/2058/granted-budget", token,
            new { grantedBudget = 10000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");

        decimal totalAllocated = 0m;
        foreach (var dept in departments.EnumerateArray())
        {
            var deptId = dept.GetProperty("departmentId").GetString();
            var allocated = dept.GetProperty("allocatedAmount").GetDecimal();

            if (deptId == deptA.ToString())
                allocated.Should().Be(6666.66m); // 6666.67 - 0.01（捨入差額）
            else if (deptId == deptB.ToString())
                allocated.Should().Be(1666.67m);
            else if (deptId == deptC.ToString())
                allocated.Should().Be(1666.67m);

            totalAllocated += allocated;
        }

        // 所有部門核定合計應等於核定總預算
        totalAllocated.Should().Be(10000m);
    }

    [Fact]
    public async Task UpdateGrantedBudget_SingleDepartment_AllocationEqualsGranted()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("單一部門分配");
        await InitializeAnnualBudgetAsync(2059, token);

        await SaveDepartmentItemsAsync(2059, deptId, token,
            BudgetItemPayload(1, "活動", "項目", 50000m));

        var response = await AuthorizedPutAsync("/annual-budgets/2059/granted-budget", token,
            new { grantedBudget = 80000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var departments = body.GetProperty("data").GetProperty("departments");

        foreach (var dept in departments.EnumerateArray())
        {
            if (dept.GetProperty("departmentId").GetString() == deptId.ToString())
                dept.GetProperty("allocatedAmount").GetDecimal().Should().Be(80000m);
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_MultipleUpdates_LastValueWins()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("多次更新部門");
        await InitializeAnnualBudgetAsync(2060, token);

        await SaveDepartmentItemsAsync(2060, deptId, token,
            BudgetItemPayload(1, "活動", "項目", 50000m));

        // 第一次核定
        await AuthorizedPutAsync("/annual-budgets/2060/granted-budget", token,
            new { grantedBudget = 50000m });

        // 第二次核定（應覆蓋）
        var response = await AuthorizedPutAsync("/annual-budgets/2060/granted-budget", token,
            new { grantedBudget = 80000m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("data").GetProperty("grantedBudget").GetDecimal().Should().Be(80000m);

        // 驗證分配也根據最後值計算
        foreach (var dept in body.GetProperty("data").GetProperty("departments").EnumerateArray())
        {
            if (dept.GetProperty("departmentId").GetString() == deptId.ToString())
                dept.GetProperty("allocatedAmount").GetDecimal().Should().Be(80000m);
        }
    }

    [Fact]
    public async Task UpdateGrantedBudget_YearOutOfRange_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPutAsync("/annual-budgets/2101/granted-budget", token,
            new { grantedBudget = 100000m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Import ──────────────────────────────────────

    [Fact]
    public async Task Import_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/annual-budgets/2026/import", new
        {
            departments = new[]
            {
                new { departmentName = "測試部門", items = new[] { new { sequence = 1, activityName = "A", contentItem = "B", amount = 100m } } }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Import_ValidData_ReturnsSuccess()
    {
        var token = await LoginAndGetTokenAsync();
        var deptName = "匯入測試部門A";
        await EnsureDepartmentAsync(deptName);
        await InitializeAnnualBudgetAsync(2061, token);

        var response = await AuthorizedPostAsync("/annual-budgets/2061/import", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = deptName,
                    items = new[]
                    {
                        new { sequence = 1, activityName = "排球營", contentItem = "場地費", amount = 10000m, note = "室內排球場" },
                        new { sequence = 2, activityName = "排球營", contentItem = "教練費", amount = 3000m, note = (string?)null },
                        new { sequence = 3, activityName = "全國賽", contentItem = "場地費", amount = 54000m, note = "三面網" }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("year").GetInt32().Should().Be(2061);
        data.GetProperty("totalDepartments").GetInt32().Should().Be(1);
        data.GetProperty("totalItems").GetInt32().Should().Be(3);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(67000m);

        var dept = data.GetProperty("departments")[0];
        dept.GetProperty("departmentName").GetString().Should().Be(deptName);
        dept.GetProperty("isNewDepartment").GetBoolean().Should().BeFalse();
        dept.GetProperty("itemCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Import_OverwriteMode_ClearsExistingItems()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("匯入覆蓋部門");
        await InitializeAnnualBudgetAsync(2062, token);

        // 先寫入 2 筆
        await SaveDepartmentItemsAsync(2062, deptId, token,
            BudgetItemPayload(1, "舊活動A", "舊項目A", 5000m),
            BudgetItemPayload(2, "舊活動B", "舊項目B", 3000m));

        // 匯入覆蓋：只有 1 筆新資料
        var response = await AuthorizedPostAsync("/annual-budgets/2062/import", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = "匯入覆蓋部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "新活動", contentItem = "新項目", amount = 10000m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證只剩 1 筆
        var itemsResp = await AuthorizedGetAsync(DepartmentItemsUrl(2062, deptId), token);
        var itemsBody = await ReadBodyAsync(itemsResp);
        var items = itemsBody.GetProperty("data");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("activityName").GetString().Should().Be("新活動");
    }

    [Fact]
    public async Task Import_NewDepartment_AutoCreates()
    {
        var token = await LoginAndGetTokenAsync();
        var uniqueName = $"自動建立部門_{Guid.NewGuid():N}"[..20];

        var response = await AuthorizedPostAsync("/annual-budgets/2063/import", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = uniqueName,
                    items = new[]
                    {
                        new { sequence = 1, activityName = "活動", contentItem = "項目", amount = 1000m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var dept = body.GetProperty("data").GetProperty("departments")[0];
        dept.GetProperty("isNewDepartment").GetBoolean().Should().BeTrue();
        dept.GetProperty("departmentId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Import_MultipleDepartments_AllImported()
    {
        var token = await LoginAndGetTokenAsync();
        await EnsureDepartmentAsync("批量匯入A部門");
        await EnsureDepartmentAsync("批量匯入B部門");
        await InitializeAnnualBudgetAsync(2064, token);

        var response = await AuthorizedPostAsync("/annual-budgets/2064/import", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = "批量匯入A部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "A活動", contentItem = "A項目", amount = 5000m, note = (string?)null }
                    }
                },
                new
                {
                    departmentName = "批量匯入B部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "B活動1", contentItem = "B項目1", amount = 3000m, note = (string?)null },
                        new { sequence = 2, activityName = "B活動2", contentItem = "B項目2", amount = 7000m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetProperty("totalDepartments").GetInt32().Should().Be(2);
        data.GetProperty("totalItems").GetInt32().Should().Be(3);
        data.GetProperty("totalAmount").GetDecimal().Should().Be(15000m);
    }

    [Fact]
    public async Task Import_EmptyDepartments_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/annual-budgets/2065/import", token, new
        {
            departments = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Import_YearOutOfRange_Returns400()
    {
        var token = await LoginAndGetTokenAsync();

        var response = await AuthorizedPostAsync("/annual-budgets/2101/import", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = "部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "A", contentItem = "B", amount = 100m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Import Preview ──────────────────────────────

    [Fact]
    public async Task ImportPreview_Unauthorized_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/annual-budgets/2026/import/preview", new
        {
            departments = new[]
            {
                new { departmentName = "測試", items = new[] { new { sequence = 1, activityName = "A", contentItem = "B", amount = 100m } } }
            }
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ImportPreview_DoesNotWriteData()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預覽不寫入部門");
        await InitializeAnnualBudgetAsync(2066, token);

        // 先寫入 1 筆
        await SaveDepartmentItemsAsync(2066, deptId, token,
            BudgetItemPayload(1, "原有活動", "原有項目", 5000m));

        // 呼叫預覽 API
        await AuthorizedPostAsync("/annual-budgets/2066/import/preview", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = "預覽不寫入部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "新活動", contentItem = "新項目", amount = 10000m, note = (string?)null }
                    }
                }
            }
        });

        // 驗證原有資料未被覆蓋
        var itemsResp = await AuthorizedGetAsync(DepartmentItemsUrl(2066, deptId), token);
        var body = await ReadBodyAsync(itemsResp);
        var items = body.GetProperty("data");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("activityName").GetString().Should().Be("原有活動");
    }

    [Fact]
    public async Task ImportPreview_WarnsAboutOverwrite()
    {
        var token = await LoginAndGetTokenAsync();
        var deptId = await EnsureDepartmentAsync("預覽警告部門");
        await InitializeAnnualBudgetAsync(2067, token);

        // 先寫入資料
        await SaveDepartmentItemsAsync(2067, deptId, token,
            BudgetItemPayload(1, "既有活動", "既有項目", 5000m));

        var response = await AuthorizedPostAsync("/annual-budgets/2067/import/preview", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = "預覽警告部門",
                    items = new[]
                    {
                        new { sequence = 1, activityName = "新", contentItem = "新", amount = 100m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        var warnings = data.GetProperty("warnings");
        warnings.GetArrayLength().Should().BeGreaterThan(0);

        var warningTexts = Enumerable.Range(0, warnings.GetArrayLength())
            .Select(i => warnings[i].GetString())
            .ToList();
        warningTexts.Should().Contain(w => w!.Contains("覆蓋"));
    }

    [Fact]
    public async Task ImportPreview_NewDepartment_ShowsWarning()
    {
        var token = await LoginAndGetTokenAsync();
        var uniqueName = $"預覽新部門_{Guid.NewGuid():N}"[..18];

        var response = await AuthorizedPostAsync("/annual-budgets/2068/import/preview", token, new
        {
            departments = new[]
            {
                new
                {
                    departmentName = uniqueName,
                    items = new[]
                    {
                        new { sequence = 1, activityName = "A", contentItem = "B", amount = 100m, note = (string?)null }
                    }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        var data = body.GetProperty("data");
        data.GetProperty("departments")[0].GetProperty("isNewDepartment").GetBoolean().Should().BeTrue();

        var warnings = data.GetProperty("warnings");
        var warningTexts = Enumerable.Range(0, warnings.GetArrayLength())
            .Select(i => warnings[i].GetString())
            .ToList();
        warningTexts.Should().Contain(w => w!.Contains("自動建立"));
    }

    // ── API Spec ────────────────────────────────────

    [Fact]
    public async Task ApiSpec_NoAuth_ReturnsEndpoints()
    {
        var response = await _client.GetAsync("/api-spec");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync(response);
        body.GetProperty("code").GetInt32().Should().Be(0);

        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("Hans-OS API");
        data.GetProperty("endpoints").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ApiSpec_ContainsImportEndpoint()
    {
        var response = await _client.GetAsync("/api-spec");
        var body = await ReadBodyAsync(response);
        var endpoints = body.GetProperty("data").GetProperty("endpoints");

        var hasImport = false;
        foreach (var ep in endpoints.EnumerateArray())
        {
            var route = ep.GetProperty("route").GetString();
            if (route is not null && route.Contains("import") && route.Contains("POST"))
            {
                hasImport = true;
                break;
            }
        }

        hasImport.Should().BeTrue("should contain the budget import endpoint");
    }

    // ── Helpers ──────────────────────────────────────

    private async Task<Guid> EnsureDepartmentAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = db.SportsDepartments.FirstOrDefault(d => d.Name == name);
        if (existing is not null)
        {
            return existing.Id;
        }

        var entity = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.SportsDepartments.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
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

    private Task<HttpResponseMessage> AuthorizedGetAsync(string url, string token)
        => _client.SendAsync(AuthGet(url, token));

    private Task<HttpResponseMessage> AuthorizedPutAsync(string url, string token, object data)
        => _client.SendAsync(AuthPut(url, token, data));

    private Task<HttpResponseMessage> AuthorizedPostAsync(string url, string token, object data)
        => _client.SendAsync(AuthPost(url, token, data));

    private Task<HttpResponseMessage> InitializeAnnualBudgetAsync(int year, string token)
        => AuthorizedGetAsync($"/annual-budgets/{year}", token);

    private Task<HttpResponseMessage> SaveDepartmentItemsAsync(int year, Guid departmentId, string token, params object[] items)
        => AuthorizedPutAsync(DepartmentItemsUrl(year, departmentId), token, new { items });

    private async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

    private static string DepartmentItemsUrl(int year, Guid departmentId)
        => $"/annual-budgets/{year}/departments/{departmentId}/items";

    private static object BudgetItemPayload(
        int sequence,
        string activityName,
        string contentItem,
        decimal amount,
        string? note = null,
        decimal? actualAmount = null,
        string? actualNote = null,
        string? id = null)
        => new
        {
            id,
            sequence,
            activityName,
            contentItem,
            amount,
            note,
            actualAmount,
            actualNote,
        };

    private static HttpRequestMessage AuthGet(string url, string token)
        => CreateAuthorizedRequest(HttpMethod.Get, url, token);

    private static HttpRequestMessage AuthPut(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Put, url, token, JsonContent.Create(data));

    private static HttpRequestMessage AuthPost(string url, string token, object data)
        => CreateAuthorizedRequest(HttpMethod.Post, url, token, JsonContent.Create(data));

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
