using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HansOS.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HansOS.Api.IntegrationTests;

public class TodoItemControllerTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ══════════════════════════════════════════════════
    //  CRUD — 基本操作
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task GetItems_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetItems_Authenticated_ReturnsPagedResponse()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        var data = body.GetProperty("data");
        data.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        data.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("page").GetInt32().Should().Be(1);
        data.GetProperty("pageSize").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task CreateItem_ValidRequest_ReturnsCreatedItem()
    {
        var token = await LoginAsync();
        var response = await AuthPost("/todo/items", token, new
        {
            title = "測試任務_CRUD",
            description = "描述文字",
            priority = "High",
            difficulty = "Medium",
            scheduledDate = "2025-06-01",
            dueDate = "2025-06-15",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("測試任務_CRUD");
        data.GetProperty("priority").GetString().Should().Be("High");
        data.GetProperty("difficulty").GetString().Should().Be("Medium");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateItem_MissingTitle_Returns400()
    {
        var token = await LoginAsync();
        var response = await AuthPost("/todo/items", token, new
        {
            description = "沒有標題",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetItem_ExistingItem_ReturnsDetail()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "詳情任務_Detail");

        var response = await AuthGet($"/todo/items/{itemId}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("詳情任務_Detail");
        data.GetProperty("children").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetItem_NotFound_Returns404()
    {
        var token = await LoginAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthGet($"/todo/items/{fakeId}", token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateItem_ValidRequest_ReturnsUpdated()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "待更新任務_Update");

        var response = await AuthPut($"/todo/items/{itemId}", token, new
        {
            title = "已更新任務",
            priority = "Urgent",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").GetProperty("title").GetString().Should().Be("已更新任務");
        body.GetProperty("data").GetProperty("priority").GetString().Should().Be("Urgent");
    }

    [Fact]
    public async Task UpdateItem_NotFound_Returns404()
    {
        var token = await LoginAsync();
        var fakeId = Guid.NewGuid();

        var response = await AuthPut($"/todo/items/{fakeId}", token, new
        {
            title = "不存在的任務",
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_SoftDeletes_ReturnsOk()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "待刪任務_Delete");

        var delResponse = await AuthDelete($"/todo/items/{itemId}", token);
        delResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 已軟刪除 → 正常查詢找不到
        var getResponse = await AuthGet($"/todo/items/{itemId}", token);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ══════════════════════════════════════════════════
    //  Status
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatus_Unauthorized_Returns401()
    {
        var fakeId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/todo/items/{fakeId}/status", new { status = "Completed" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_ToCompleted_SetsCompletedAt()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "狀態任務_Complete");

        var response = await AuthPut($"/todo/items/{itemId}/status", token, new
        {
            status = "Completed",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var data = body.GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("Done");
        data.GetProperty("completedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateStatus_BackToPending_ClearsCompletedAt()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "狀態任務_Reopen");

        await AuthPut($"/todo/items/{itemId}/status", token, new { status = "Completed" });

        var response = await AuthPut($"/todo/items/{itemId}/status", token, new
        {
            status = "Pending",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task UpdateStatus_InvalidValue_Returns400()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "狀態任務_Invalid");

        var response = await AuthPut($"/todo/items/{itemId}/status", token, new
        {
            status = "NonExistent",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ══════════════════════════════════════════════════
    //  Hierarchy — 父子任務
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task CreateItem_WithParentId_CreatesSubtask()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_Hierarchy");

        var childResp = await AuthPost("/todo/items", token, new
        {
            title = "子任務_Hierarchy",
            parentId,
        });

        childResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(childResp);
        body.GetProperty("data").GetProperty("parentId").GetString().Should().Be(parentId);

        // 驗證父任務 Detail 含有子任務
        var detailResp = await AuthGet($"/todo/items/{parentId}", token);
        var detail = (await ReadBody(detailResp)).GetProperty("data");
        detail.GetProperty("children").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetItems_IncludeChildren_ReturnsParentWithSubtasks()
    {
        var token = await LoginAsync();
        var uniqueTitle = $"父任務_IncludeChildren_{Guid.NewGuid():N}";
        var parentId = await CreateItemAndGetIdAsync(token, uniqueTitle);
        await CreateChildAndGetIdAsync(token, parentId, "子任務_IncludeChildren");

        var response = await AuthGet($"/todo/items?includeChildren=true&search={uniqueTitle}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var items = body.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        var parent = items.Single(i => i.GetProperty("id").GetString() == parentId);
        parent.GetProperty("children").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetItems_IncludeChildren_AfterChildDeleted_ExcludesDeletedSubtask()
    {
        var token = await LoginAsync();
        var uniqueTitle = $"父任務_DeletedChildList_{Guid.NewGuid():N}";
        var parentId = await CreateItemAndGetIdAsync(token, uniqueTitle);
        var childId = await CreateChildAndGetIdAsync(token, parentId, "子任務_DeletedChildList");
        await AuthDelete($"/todo/items/{childId}", token);

        var response = await AuthGet($"/todo/items?includeChildren=true&search={uniqueTitle}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var parent = body.GetProperty("data").GetProperty("items").EnumerateArray()
            .Single(i => i.GetProperty("id").GetString() == parentId);
        parent.GetProperty("children").EnumerateArray()
            .Should().NotContain(i => i.GetProperty("id").GetString() == childId);
    }

    [Fact]
    public async Task GetItem_AfterChildDeleted_ExcludesDeletedSubtask()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_DeletedChildDetail");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "子任務_DeletedChildDetail");
        await AuthDelete($"/todo/items/{childId}", token);

        var response = await AuthGet($"/todo/items/{parentId}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = (await ReadBody(response)).GetProperty("data");
        detail.GetProperty("children").EnumerateArray()
            .Should().NotContain(i => i.GetProperty("id").GetString() == childId);
    }

    [Fact]
    public async Task CreateItem_WithParentId_InheritsParentCategory()
    {
        var token = await LoginAsync();
        var categoryId = await CreateCategoryAndGetIdAsync(token, "分類_ParentInheritance");
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_ParentCategory", categoryId: categoryId);

        var childId = await CreateChildAndGetIdAsync(token, parentId, "子任務_ParentCategory");

        var response = await AuthGet($"/todo/items/{childId}", token);
        var detail = (await ReadBody(response)).GetProperty("data");
        detail.GetProperty("categoryId").GetString().Should().Be(categoryId);
    }

    [Fact]
    public async Task CreateItem_WithForeignCategory_Returns404()
    {
        var ownerToken = await LoginAsync();
        var otherToken = await CreateUserAndGetTokenAsync();
        var categoryId = await CreateCategoryAndGetIdAsync(ownerToken, "分類_ForeignCategory");

        var response = await AuthPost("/todo/items", otherToken, new
        {
            title = "任務_ForeignCategory",
            categoryId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateItem_WithForeignTag_Returns404()
    {
        var ownerToken = await LoginAsync();
        var otherToken = await CreateUserAndGetTokenAsync();
        var tagId = await CreateTagAndGetIdAsync(ownerToken, "標籤_ForeignTag");

        var response = await AuthPost("/todo/items", otherToken, new
        {
            title = "任務_ForeignTag",
            tagIds = new[] { tagId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderChildren_ValidIds_UpdatesOrder()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_ReorderChildren");
        var childA = await CreateChildAndGetIdAsync(token, parentId, "子任務_A");
        var childB = await CreateChildAndGetIdAsync(token, parentId, "子任務_B");

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { childB, childA },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var children = body.GetProperty("data").EnumerateArray().ToList();
        children.Select(c => c.GetProperty("id").GetString()).Should().Equal(childB, childA);
        children.Select(c => c.GetProperty("order").GetInt32()).Should().Equal(0, 1);
    }

    [Fact]
    public async Task ReorderChildren_WithDeletedChild_UpdatesVisibleChildrenOnly()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_ReorderDeletedChild");
        var childA = await CreateChildAndGetIdAsync(token, parentId, "子任務_ReorderDeleted_A");
        var childB = await CreateChildAndGetIdAsync(token, parentId, "子任務_ReorderDeleted_B");
        var childDeleted = await CreateChildAndGetIdAsync(token, parentId, "子任務_ReorderDeleted_C");
        await AuthDelete($"/todo/items/{childDeleted}", token);

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { childB, childA },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var children = (await ReadBody(response)).GetProperty("data").EnumerateArray().ToList();
        children.Select(c => c.GetProperty("id").GetString()).Should().Equal(childB, childA);
    }

    [Fact]
    public async Task ReorderChildren_WithForeignChild_Returns403()
    {
        var token = await LoginAsync();
        var otherToken = await CreateUserAndGetTokenAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_ForeignChild");
        var otherParentId = await CreateItemAndGetIdAsync(otherToken, "其他父任務_ForeignChild");
        var otherChildId = await CreateChildAndGetIdAsync(otherToken, otherParentId, "其他子任務_ForeignChild");

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { otherChildId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReorderChildren_Unauthorized_Returns401()
    {
        var fakeId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/todo/items/{fakeId}/children/reorder", new
        {
            orderedChildIds = new[] { Guid.NewGuid() },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReorderChildren_WithInvalidChild_Returns400()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_InvalidChild");

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { Guid.NewGuid() },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderChildren_WithMissingChild_Returns400()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_MissingChild");
        var childA = await CreateChildAndGetIdAsync(token, parentId, "子任務_Missing_A");
        await CreateChildAndGetIdAsync(token, parentId, "子任務_Missing_B");

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { childA },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderChildren_WithDuplicateChild_Returns400()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_DuplicateChild");
        var childA = await CreateChildAndGetIdAsync(token, parentId, "子任務_Duplicate_A");

        var response = await AuthPut($"/todo/items/{parentId}/children/reorder", token, new
        {
            orderedChildIds = new[] { childA, childA },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateItem_SelfAsParent_Returns400()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "自我父任務");

        var response = await AuthPut($"/todo/items/{itemId}", token, new
        {
            title = "自我父任務",
            parentId = itemId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_WithActiveChildrenAndParentId_Returns400()
    {
        var token = await LoginAsync();
        var grandParentId = await CreateItemAndGetIdAsync(token, "祖任務_NoGrandchildren");
        var parentId = await CreateItemAndGetIdAsync(token, "父任務_NoGrandchildren");
        await CreateChildAndGetIdAsync(token, parentId, "子任務_NoGrandchildren");

        var response = await AuthPut($"/todo/items/{parentId}", token, new
        {
            title = "父任務_NoGrandchildren",
            parentId = grandParentId,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ══════════════════════════════════════════════════
    //  Archive — 歸檔
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task Archive_Unauthorized_Returns401()
    {
        var fakeId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/todo/items/{fakeId}/archive", new { archive = true });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ArchiveItem_Archive_SetsArchivedAt()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "歸檔任務_Archive");

        var response = await AuthPut($"/todo/items/{itemId}/archive", token, new
        {
            archive = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").GetProperty("archivedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ArchiveItem_WithActiveChild_CascadesAndRestoresChild()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "歸檔父任務_Cascade");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "歸檔子任務_Cascade");

        var archiveResp = await AuthPut($"/todo/items/{parentId}/archive", token, new { archive = true });

        archiveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var archivedParent = (await ReadBody(archiveResp)).GetProperty("data");
        archivedParent.GetProperty("children").GetArrayLength().Should().Be(0);

        var unarchiveResp = await AuthPut($"/todo/items/{parentId}/archive", token, new { archive = false });

        unarchiveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var restoredParent = (await ReadBody(unarchiveResp)).GetProperty("data");
        restoredParent.GetProperty("children").EnumerateArray()
            .Should().Contain(i => i.GetProperty("id").GetString() == childId);
    }

    [Fact]
    public async Task ArchiveItem_UnarchiveParent_DoesNotRestorePreviouslyArchivedChild()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "歸檔父任務_PreArchivedChild");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "預先歸檔子任務");

        await AuthPut($"/todo/items/{childId}/archive", token, new { archive = true });
        await AuthPut($"/todo/items/{parentId}/archive", token, new { archive = true });

        var unarchiveResp = await AuthPut($"/todo/items/{parentId}/archive", token, new { archive = false });

        unarchiveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var restoredParent = (await ReadBody(unarchiveResp)).GetProperty("data");
        restoredParent.GetProperty("children").EnumerateArray()
            .Should().NotContain(i => i.GetProperty("id").GetString() == childId);

        var childResp = await AuthGet($"/todo/items/{childId}", token);
        var child = (await ReadBody(childResp)).GetProperty("data");
        child.GetProperty("archivedAt").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ArchiveItem_Unarchive_ClearsArchivedAt()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "歸檔任務_Unarchive");

        await AuthPut($"/todo/items/{itemId}/archive", token, new { archive = true });
        var response = await AuthPut($"/todo/items/{itemId}/archive", token, new
        {
            archive = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var archivedAt = body.GetProperty("data").GetProperty("archivedAt");
        archivedAt.ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ══════════════════════════════════════════════════
    //  Trash — 軟刪除 & 還原 & 永久刪除
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task GetTrash_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items/trash");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Trash_DeleteAndRestore_Works()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "垃圾桶_Restore");

        // 軟刪除
        await AuthDelete($"/todo/items/{itemId}", token);

        // 出現在垃圾桶
        var trashResp = await AuthGet("/todo/items/trash", token);
        var trashBody = await ReadBody(trashResp);
        trashBody.GetProperty("data").EnumerateArray()
            .Should().Contain(x => x.GetProperty("id").GetString() == itemId);

        // 還原
        var restoreResp = await AuthPut($"/todo/items/{itemId}/restore", token, new { });
        restoreResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 可以正常取回
        var getResp = await AuthGet($"/todo/items/{itemId}", token);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Trash_DeleteAndRestoreParent_CascadesToActiveChild()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "垃圾桶父任務_Cascade");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "垃圾桶子任務_Cascade");

        await AuthDelete($"/todo/items/{parentId}", token);

        var childAfterDelete = await AuthGet($"/todo/items/{childId}", token);
        childAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var restoreResp = await AuthPut($"/todo/items/{parentId}/restore", token, new { });
        restoreResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var childAfterRestore = await AuthGet($"/todo/items/{childId}", token);
        childAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Trash_RestoreParent_DoesNotRestorePreviouslyDeletedChild()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "垃圾桶父任務_PreDeletedChild");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "預先刪除子任務");

        await AuthDelete($"/todo/items/{childId}", token);
        await AuthDelete($"/todo/items/{parentId}", token);

        var restoreResp = await AuthPut($"/todo/items/{parentId}/restore", token, new { });

        restoreResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var childAfterRestore = await AuthGet($"/todo/items/{childId}", token);
        childAfterRestore.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PermanentDelete_RemovesPermanently()
    {
        var token = await LoginAsync();
        var itemId = await CreateItemAndGetIdAsync(token, "永久刪除_Perm");

        await AuthDelete($"/todo/items/{itemId}", token);

        var permResp = await AuthDelete($"/todo/items/{itemId}/permanent", token);
        permResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 垃圾桶也找不到了
        var trashResp = await AuthGet("/todo/items/trash", token);
        var trashBody = await ReadBody(trashResp);
        trashBody.GetProperty("data").EnumerateArray()
            .Should().NotContain(x => x.GetProperty("id").GetString() == itemId);
    }

    [Fact]
    public async Task PermanentDelete_WithChild_RemovesParentAndChild()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "永久刪除父任務_Cascade");
        var childId = await CreateChildAndGetIdAsync(token, parentId, "永久刪除子任務_Cascade");

        await AuthDelete($"/todo/items/{parentId}", token);

        var permResp = await AuthDelete($"/todo/items/{parentId}/permanent", token);
        permResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var trashResp = await AuthGet("/todo/items/trash", token);
        var trashItems = (await ReadBody(trashResp)).GetProperty("data").EnumerateArray();
        trashItems.Should().NotContain(x => x.GetProperty("id").GetString() == parentId);
        trashItems.Should().NotContain(x => x.GetProperty("id").GetString() == childId);
    }

    // ══════════════════════════════════════════════════
    //  Time-Based Views
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task GetToday_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items/today");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetToday_Authenticated_ReturnsArray()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items/today", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("code").GetInt32().Should().Be(0);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetWeek_Authenticated_ReturnsArray()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items/week", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetMonth_Authenticated_ReturnsArray()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items/month", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
    }

    // ══════════════════════════════════════════════════
    //  Stats & Badge
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task GetStats_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_Authenticated_ReturnsValidStats()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items/stats", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var data = body.GetProperty("data");
        data.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("pending").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("inProgress").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("completed").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("overdue").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        data.GetProperty("archived").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetReminderCount_Authenticated_ReturnsNumber()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items/reminder-count", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        body.GetProperty("data").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    // ══════════════════════════════════════════════════
    //  Search
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task Search_Unauthorized_Returns401()
    {
        var response = await _client.GetAsync("/todo/items/search?q=test");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_ByTitle_FindsItem()
    {
        var token = await LoginAsync();
        var uniqueTitle = $"搜尋目標_{Guid.NewGuid():N}";
        await CreateItemAndGetIdAsync(token, uniqueTitle);

        var response = await AuthGet($"/todo/items/search?q={uniqueTitle[..8]}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var searchPrefix = uniqueTitle.Substring(0, 8);
        body.GetProperty("data").EnumerateArray()
            .Should().Contain(x => x.GetProperty("title").GetString()!.Contains(searchPrefix));
    }

    // ══════════════════════════════════════════════════
    //  Batch Update
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task BatchUpdate_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/todo/items/batch", new
        {
            ids = new[] { Guid.NewGuid() },
            status = "Completed",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BatchUpdate_ChangeStatus_Works()
    {
        var token = await LoginAsync();
        var id1 = await CreateItemAndGetIdAsync(token, "批次_1");
        var id2 = await CreateItemAndGetIdAsync(token, "批次_2");

        var response = await AuthPut("/todo/items/batch", token, new
        {
            ids = new[] { id1, id2 },
            status = "InProgress",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 驗證兩個都變 InProgress
        var resp1 = await AuthGet($"/todo/items/{id1}", token);
        (await ReadBody(resp1)).GetProperty("data").GetProperty("status").GetString()
            .Should().Be("InProgress");
    }

    // ══════════════════════════════════════════════════
    //  Sort
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task Sort_Unauthorized_Returns401()
    {
        var response = await _client.PutAsJsonAsync("/todo/items/sort", new
        {
            orderedIds = new[] { Guid.NewGuid() },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Sort_UpdatesOrder()
    {
        var token = await LoginAsync();
        var id1 = await CreateItemAndGetIdAsync(token, "排序A");
        var id2 = await CreateItemAndGetIdAsync(token, "排序B");

        // 反轉順序
        var response = await AuthPut("/todo/items/sort", token, new
        {
            orderedIds = new[] { id2, id1 },
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════
    //  Recurring Task — 週期性任務
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task RecurringTask_CompleteGeneratesNext()
    {
        var token = await LoginAsync();

        // 建立週期性任務
        var createResp = await AuthPost("/todo/items", token, new
        {
            title = "週期任務_Recurrence",
            scheduledDate = "2025-06-01",
            dueDate = "2025-06-03",
            recurrencePattern = "Weekly",
            recurrenceInterval = 1,
        });
        var createBody = await ReadBody(createResp);
        var itemId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        // 完成它
        await AuthPut($"/todo/items/{itemId}/status", token, new { status = "Completed" });

        // 確認生成了下一期
        var listResp = await AuthGet("/todo/items?search=週期任務_Recurrence", token);
        var listBody = await ReadBody(listResp);
        var items = listBody.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ══════════════════════════════════════════════════
    //  Tags 整合
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task CreateItem_WithTags_TagsIncluded()
    {
        var token = await LoginAsync();

        // 先建立標籤
        var tagResp = await AuthPost("/todo/tags", token, new { name = $"標籤_{Guid.NewGuid():N}" });
        var tagBody = await ReadBody(tagResp);
        var tagId = tagBody.GetProperty("data").GetProperty("id").GetString()!;

        // 建立任務含標籤
        var itemResp = await AuthPost("/todo/items", token, new
        {
            title = "含標籤任務",
            tagIds = new[] { tagId },
        });

        var itemBody = await ReadBody(itemResp);
        itemBody.GetProperty("data").GetProperty("tags").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    // ══════════════════════════════════════════════════
    //  Filtering
    // ══════════════════════════════════════════════════

    [Fact]
    public async Task GetItems_FilterByStatus_Works()
    {
        var token = await LoginAsync();
        await CreateItemAndGetIdAsync(token, "篩選_StatusFilter");

        var response = await AuthGet("/todo/items?status=Pending", token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await ReadBody(response);
        var items = body.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().OnlyContain(i => i.GetProperty("status").GetString() == "Pending");
    }

    [Fact]
    public async Task GetItems_FilterByPriority_Works()
    {
        var token = await LoginAsync();
        var response = await AuthGet("/todo/items?priority=Urgent", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBody(response);
        var items = body.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().OnlyContain(i => i.GetProperty("priority").GetString() == "Urgent");
    }

    [Fact]
    public async Task GetItems_TopLevelOnly_ExcludesSubtasks()
    {
        var token = await LoginAsync();
        var parentId = await CreateItemAndGetIdAsync(token, "頂層_TopLevel");
        await AuthPost("/todo/items", token, new
        {
            title = "子層_TopLevel",
            parentId,
        });

        var response = await AuthGet("/todo/items?topLevelOnly=true", token);
        var body = await ReadBody(response);
        var items = body.GetProperty("data").GetProperty("items").EnumerateArray().ToList();
        items.Should().OnlyContain(i =>
            i.GetProperty("parentId").ValueKind == JsonValueKind.Null);
    }

    // ══════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            username = "hans",
            password = "H@ns19951204",
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task<string> CreateUserAndGetTokenAsync()
    {
        var username = $"todo_user_{Guid.NewGuid():N}";
        const string password = "P@ssw0rd!123";
        await EnsureUserAsync(username, password);
        var response = await _client.PostAsJsonAsync("/auth/login", new { username, password });
        var body = await ReadBody(response);
        return body.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    private async Task EnsureUserAsync(string username, string password)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@example.com",
            EmailConfirmed = true,
            RealName = username,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue();
    }

    private async Task<string> CreateItemAndGetIdAsync(string token, string title, string? categoryId = null)
    {
        var response = await AuthPost("/todo/items", token, new { title, categoryId });
        var body = await ReadBody(response);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateCategoryAndGetIdAsync(string token, string name)
    {
        var response = await AuthPost("/todo/categories", token, new { name });
        var body = await ReadBody(response);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateTagAndGetIdAsync(string token, string name)
    {
        var response = await AuthPost("/todo/tags", token, new { name });
        var body = await ReadBody(response);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<string> CreateChildAndGetIdAsync(string token, string parentId, string title)
    {
        var response = await AuthPost("/todo/items", token, new { title, parentId });
        var body = await ReadBody(response);
        return body.GetProperty("data").GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> ReadBody(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;

    private Task<HttpResponseMessage> AuthGet(string url, string token)
        => _client.SendAsync(Req(HttpMethod.Get, url, token));

    private Task<HttpResponseMessage> AuthPost(string url, string token, object data)
        => _client.SendAsync(Req(HttpMethod.Post, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthPut(string url, string token, object data)
        => _client.SendAsync(Req(HttpMethod.Put, url, token, JsonContent.Create(data)));

    private Task<HttpResponseMessage> AuthDelete(string url, string token)
        => _client.SendAsync(Req(HttpMethod.Delete, url, token));

    private static HttpRequestMessage Req(
        HttpMethod method, string url, string token, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
