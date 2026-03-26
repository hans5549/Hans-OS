using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.TsfSettings;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.UnitTests;

public class TsfSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly TsfSettingsService _sut;

    public TsfSettingsServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(dbOptions);
        _sut = new TsfSettingsService(_db);
    }

    // ── GetDepartmentsAsync ─────────────────────────

    [Fact]
    public async Task GetDepartments_ReturnsList()
    {
        await SeedDepartmentAsync("桌球部");
        await SeedDepartmentAsync("籃球部");
        await SeedDepartmentAsync("田徑部");

        var result = await _sut.GetDepartmentsAsync();

        result.Should().HaveCount(3);
        result.Select(d => d.Name).Should().Contain("桌球部");
        result.Select(d => d.Name).Should().Contain("籃球部");
        result.Select(d => d.Name).Should().Contain("田徑部");
    }

    // ── CreateDepartmentAsync ───────────────────────

    [Fact]
    public async Task CreateDepartment_ValidData_ReturnsCreated()
    {
        var request = new CreateDepartmentRequest("羽球部", "羽球相關活動");

        var result = await _sut.CreateDepartmentAsync(request);

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("羽球部");
        result.Note.Should().Be("羽球相關活動");

        var saved = await _db.SportsDepartments.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_ThrowsError()
    {
        await SeedDepartmentAsync("重複部門");

        var request = new CreateDepartmentRequest("重複部門", null);
        var act = () => _sut.CreateDepartmentAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*已存在*");
    }

    // ── UpdateDepartmentAsync ───────────────────────

    [Fact]
    public async Task UpdateDepartment_ValidData_UpdatesSuccessfully()
    {
        var id = await SeedDepartmentAsync("原始名稱");

        var request = new UpdateDepartmentRequest("更新名稱", "新備註");
        await _sut.UpdateDepartmentAsync(id, request);

        var updated = await _db.SportsDepartments.FindAsync(id);
        updated!.Name.Should().Be("更新名稱");
        updated.Note.Should().Be("新備註");
    }

    [Fact]
    public async Task UpdateDepartment_NotFound_ThrowsError()
    {
        var request = new UpdateDepartmentRequest("不存在", null);
        var act = () => _sut.UpdateDepartmentAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteDepartmentAsync ───────────────────────

    [Fact]
    public async Task DeleteDepartment_ValidId_DeletesSuccessfully()
    {
        var id = await SeedDepartmentAsync("待刪除");

        await _sut.DeleteDepartmentAsync(id);

        var deleted = await _db.SportsDepartments.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDepartment_NotFound_ThrowsError()
    {
        var act = () => _sut.DeleteDepartmentAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetBankInitialBalancesAsync ─────────────────

    [Fact]
    public async Task GetBankInitialBalances_ReturnsSortedList()
    {
        await SeedBankBalanceAsync("合作金庫", 50000m);
        await SeedBankBalanceAsync("上海銀行", 30000m);

        var result = await _sut.GetBankInitialBalancesAsync();

        result.Should().HaveCount(2);
        result[0].BankName.Should().Be("上海銀行");
        result[1].BankName.Should().Be("合作金庫");
    }

    // ── UpdateBankInitialBalanceAsync ────────────────

    [Fact]
    public async Task UpdateBankInitialBalance_ValidData_Updates()
    {
        var id = await SeedBankBalanceAsync("上海銀行", 10000m);

        var request = new UpdateBankInitialBalanceRequest(99999.99m);
        await _sut.UpdateBankInitialBalanceAsync(id, request);

        var updated = await _db.BankInitialBalances.FindAsync(id);
        updated!.InitialAmount.Should().Be(99999.99m);
    }

    [Fact]
    public async Task UpdateBankInitialBalance_NotFound_ThrowsError()
    {
        var request = new UpdateBankInitialBalanceRequest(100m);
        var act = () => _sut.UpdateBankInitialBalanceAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Helpers ─────────────────────────────────────

    private async Task<Guid> SeedDepartmentAsync(string name, string? note = null)
    {
        var entity = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            Note = note,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.SportsDepartments.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<Guid> SeedBankBalanceAsync(string bankName, decimal amount)
    {
        var entity = new BankInitialBalance
        {
            Id = Guid.NewGuid(),
            BankName = bankName,
            InitialAmount = amount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.BankInitialBalances.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
