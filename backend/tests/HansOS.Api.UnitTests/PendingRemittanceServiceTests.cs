using FluentAssertions;

using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.PendingRemittances;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.UnitTests;

public class PendingRemittanceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly PendingRemittanceService _sut;

    public PendingRemittanceServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(dbOptions);
        _sut = new PendingRemittanceService(_db);
    }

    // ── GetAllAsync ─────────────────────────────────

    [Fact]
    public async Task GetAll_NoFilter_ReturnsAllItems()
    {
        await SeedRemittanceAsync(description: "匯款A");
        await SeedRemittanceAsync(description: "匯款B", status: PendingRemittanceStatus.Completed);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_FilterPending_ReturnsOnlyPending()
    {
        await SeedRemittanceAsync(description: "待處理匯款", status: PendingRemittanceStatus.Pending);
        await SeedRemittanceAsync(description: "已完成匯款", status: PendingRemittanceStatus.Completed);

        var result = await _sut.GetAllAsync(PendingRemittanceStatus.Pending);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("待處理匯款");
    }

    [Fact]
    public async Task GetAll_FilterCompleted_ReturnsOnlyCompleted()
    {
        await SeedRemittanceAsync(description: "待處理匯款", status: PendingRemittanceStatus.Pending);
        await SeedRemittanceAsync(description: "已完成匯款", status: PendingRemittanceStatus.Completed);

        var result = await _sut.GetAllAsync(PendingRemittanceStatus.Completed);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("已完成匯款");
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_OrderedByCreatedAtDesc()
    {
        var older = new PendingRemittance
        {
            Id = Guid.NewGuid(),
            Description = "較早匯款",
            Amount = 1000m,
            SourceAccount = "上海銀行",
            TargetAccount = "合作金庫",
            Status = PendingRemittanceStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2),
        };
        var newer = new PendingRemittance
        {
            Id = Guid.NewGuid(),
            Description = "較新匯款",
            Amount = 2000m,
            SourceAccount = "上海銀行",
            TargetAccount = "合作金庫",
            Status = PendingRemittanceStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.PendingRemittances.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result[0].Description.Should().Be("較新匯款");
        result[1].Description.Should().Be("較早匯款");
    }

    // ── GetByIdAsync ────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsItem()
    {
        var id = await SeedRemittanceAsync(description: "測試查詢匯款", amount: 5000m);

        var result = await _sut.GetByIdAsync(id);

        result.Id.Should().Be(id);
        result.Description.Should().Be("測試查詢匯款");
        result.Amount.Should().Be(5000m);
    }

    [Fact]
    public async Task GetById_NonExistingId_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── CreateAsync ─────────────────────────────────

    [Fact]
    public async Task Create_ValidData_ReturnsCreatedItem()
    {
        var request = new CreatePendingRemittanceRequest(
            Description: "新增匯款",
            Amount: 25000m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: null,
            RecipientName: "王小明",
            ExpectedDate: new DateOnly(2026, 6, 1),
            Note: "備註文字",
            ActivityExpenseId: null);

        var result = await _sut.CreateAsync(request);

        result.Id.Should().NotBeEmpty();
        result.Description.Should().Be("新增匯款");
        result.Amount.Should().Be(25000m);
        result.SourceAccount.Should().Be("上海銀行");
        result.TargetAccount.Should().Be("合作金庫");
        result.RecipientName.Should().Be("王小明");
        result.ExpectedDate.Should().Be(new DateOnly(2026, 6, 1));
        result.Note.Should().Be("備註文字");
        result.Status.Should().Be(PendingRemittanceStatus.Pending);
    }

    [Fact]
    public async Task Create_ValidData_SavesToDB()
    {
        var request = new CreatePendingRemittanceRequest(
            Description: "存入資料庫測試",
            Amount: 8000m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: null,
            RecipientName: null,
            ExpectedDate: null,
            Note: null,
            ActivityExpenseId: null);

        var result = await _sut.CreateAsync(request);

        var saved = await _db.PendingRemittances.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Description.Should().Be("存入資料庫測試");
        saved.Amount.Should().Be(8000m);
    }

    [Fact]
    public async Task Create_WithDepartment_LinksDepartmentCorrectly()
    {
        var deptId = await SeedDepartmentAsync("活動組");
        var request = new CreatePendingRemittanceRequest(
            Description: "部門匯款",
            Amount: 15000m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: deptId,
            RecipientName: null,
            ExpectedDate: null,
            Note: null,
            ActivityExpenseId: null);

        var result = await _sut.CreateAsync(request);

        result.DepartmentId.Should().Be(deptId);
        result.DepartmentName.Should().Be("活動組");
    }

    [Fact]
    public async Task Create_InvalidDepartment_ThrowsArgumentException()
    {
        var request = new CreatePendingRemittanceRequest(
            Description: "無效部門測試",
            Amount: 5000m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: Guid.NewGuid(),
            RecipientName: null,
            ExpectedDate: null,
            Note: null,
            ActivityExpenseId: null);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*部門不存在*");
    }

    [Fact]
    public async Task Create_TrimsWhitespace()
    {
        var request = new CreatePendingRemittanceRequest(
            Description: "  空白測試  ",
            Amount: 3000m,
            SourceAccount: "  上海銀行  ",
            TargetAccount: "  合作金庫  ",
            DepartmentId: null,
            RecipientName: "  王小明  ",
            ExpectedDate: null,
            Note: "  備註  ",
            ActivityExpenseId: null);

        var result = await _sut.CreateAsync(request);

        result.Description.Should().Be("空白測試");
        result.SourceAccount.Should().Be("上海銀行");
        result.TargetAccount.Should().Be("合作金庫");
        result.RecipientName.Should().Be("王小明");
        result.Note.Should().Be("備註");
    }

    // ── UpdateAsync ─────────────────────────────────

    [Fact]
    public async Task Update_ValidData_UpdatesFields()
    {
        var id = await SeedRemittanceAsync(description: "原始描述", amount: 1000m);
        var request = new UpdatePendingRemittanceRequest(
            Description: "更新後描述",
            Amount: 9999m,
            SourceAccount: "國泰世華",
            TargetAccount: "中國信託",
            DepartmentId: null,
            RecipientName: "李大華",
            ExpectedDate: new DateOnly(2026, 12, 25),
            Note: "更新備註",
            ActivityExpenseId: null);

        await _sut.UpdateAsync(id, request);

        var updated = await _db.PendingRemittances.FindAsync(id);
        updated!.Description.Should().Be("更新後描述");
        updated.Amount.Should().Be(9999m);
        updated.SourceAccount.Should().Be("國泰世華");
        updated.TargetAccount.Should().Be("中國信託");
        updated.RecipientName.Should().Be("李大華");
        updated.ExpectedDate.Should().Be(new DateOnly(2026, 12, 25));
        updated.Note.Should().Be("更新備註");
    }

    [Fact]
    public async Task Update_NonExistingId_ThrowsKeyNotFoundException()
    {
        var request = new UpdatePendingRemittanceRequest(
            Description: "不存在",
            Amount: 100m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: null,
            RecipientName: null,
            ExpectedDate: null,
            Note: null,
            ActivityExpenseId: null);

        var act = () => _sut.UpdateAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Update_InvalidDepartment_ThrowsArgumentException()
    {
        var id = await SeedRemittanceAsync();
        var request = new UpdatePendingRemittanceRequest(
            Description: "部門不存在測試",
            Amount: 5000m,
            SourceAccount: "上海銀行",
            TargetAccount: "合作金庫",
            DepartmentId: Guid.NewGuid(),
            RecipientName: null,
            ExpectedDate: null,
            Note: null,
            ActivityExpenseId: null);

        var act = () => _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*部門不存在*");
    }

    // ── DeleteAsync ─────────────────────────────────

    [Fact]
    public async Task Delete_ExistingId_RemovesFromDB()
    {
        var id = await SeedRemittanceAsync(description: "待刪除匯款");

        await _sut.DeleteAsync(id);

        var deleted = await _db.PendingRemittances.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistingId_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── CompleteAsync ───────────────────────────────

    [Fact]
    public async Task Complete_PendingItem_MarksCompleted()
    {
        var id = await SeedRemittanceAsync(status: PendingRemittanceStatus.Pending);

        await _sut.CompleteAsync(id, new CompletePendingRemittanceRequest("測試銀行", new DateOnly(2026, 4, 9)));

        var entity = await _db.PendingRemittances.FindAsync(id);
        entity!.Status.Should().Be(PendingRemittanceStatus.Completed);
    }

    [Fact]
    public async Task Complete_SetsCompletedAtTimestamp()
    {
        var id = await SeedRemittanceAsync(status: PendingRemittanceStatus.Pending);
        var before = DateTime.UtcNow;

        await _sut.CompleteAsync(id, new CompletePendingRemittanceRequest("測試銀行", new DateOnly(2026, 4, 9)));

        var entity = await _db.PendingRemittances.FindAsync(id);
        entity!.CompletedAt.Should().NotBeNull();
        entity.CompletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_ThrowsArgumentException()
    {
        var id = await SeedRemittanceAsync(status: PendingRemittanceStatus.Completed);

        var act = () => _sut.CompleteAsync(id, new CompletePendingRemittanceRequest("測試銀行", new DateOnly(2026, 4, 9)));

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*已完成*");
    }

    [Fact]
    public async Task Complete_NonExistingId_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.CompleteAsync(Guid.NewGuid(), new CompletePendingRemittanceRequest("測試銀行", new DateOnly(2026, 4, 9)));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Helpers ─────────────────────────────────────

    private async Task<Guid> SeedRemittanceAsync(
        string description = "測試匯款",
        decimal amount = 10000m,
        string sourceAccount = "上海銀行",
        string targetAccount = "合作金庫",
        PendingRemittanceStatus status = PendingRemittanceStatus.Pending,
        Guid? departmentId = null)
    {
        var entity = new PendingRemittance
        {
            Id = Guid.NewGuid(),
            Description = description,
            Amount = amount,
            SourceAccount = sourceAccount,
            TargetAccount = targetAccount,
            DepartmentId = departmentId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.PendingRemittances.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<Guid> SeedDepartmentAsync(string name = "活動組")
    {
        var dept = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
        };
        _db.SportsDepartments.Add(dept);
        await _db.SaveChangesAsync();
        return dept.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
