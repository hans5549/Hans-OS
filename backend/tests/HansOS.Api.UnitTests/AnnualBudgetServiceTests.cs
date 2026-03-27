using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.AnnualBudget;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace HansOS.Api.UnitTests;

public class AnnualBudgetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AnnualBudgetService _sut;

    public AnnualBudgetServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(dbOptions);
        _sut = new AnnualBudgetService(_db, NullLogger<AnnualBudgetService>.Instance);
    }

    // ── GetOverviewAsync ────────────────────────────

    [Fact]
    public async Task GetOverview_AutoInitializesBudget()
    {
        // Arrange
        var deptId = await SeedDepartmentAsync("體育組");

        // Act
        var result = await _sut.GetOverviewAsync(2025);

        // Assert
        result.Year.Should().Be(2025);
        result.Status.Should().Be("Draft");
        result.Departments.Should().HaveCount(1);
        result.Departments[0].DepartmentName.Should().Be("體育組");

        var budgetInDb = await _db.AnnualBudgets.FirstOrDefaultAsync(b => b.Year == 2025);
        budgetInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverview_InvalidYear_ThrowsArgumentException()
    {
        var act = () => _sut.GetOverviewAsync(1999);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*2020*2100*");
    }

    // ── UpdateStatusAsync ───────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidTransition_UpdatesStatus()
    {
        // Arrange
        await SeedBudgetAsync(2025, BudgetStatus.Draft);

        // Act
        await _sut.UpdateStatusAsync(2025, "Submitted");

        // Assert
        var budget = await _db.AnnualBudgets.FirstAsync(b => b.Year == 2025);
        budget.Status.Should().Be(BudgetStatus.Submitted);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ThrowsArgumentException()
    {
        // Arrange
        await SeedBudgetAsync(2025, BudgetStatus.Draft);

        // Act
        var act = () => _sut.UpdateStatusAsync(2025, "Approved");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateStatus_BudgetNotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateStatusAsync(2025, "Submitted");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── SaveDepartmentItemsAsync ────────────────────

    [Fact]
    public async Task SaveDepartmentItems_UpsertLogic_CreatesAndUpdates()
    {
        // Arrange
        var deptId = await SeedDepartmentAsync("體育組");
        await _sut.GetOverviewAsync(2025); // auto-initialize budget + department budgets

        var initialItems = new SaveBudgetItemsRequest([
            new BudgetItemInput(null, 1, "活動A", "項目A", 1000, null, null, null),
            new BudgetItemInput(null, 2, "活動B", "項目B", 2000, null, null, null),
        ]);

        var firstResult = await _sut.SaveDepartmentItemsAsync(2025, deptId, initialItems);
        var existingItemId = firstResult[0].Id;

        // Act — update item 1, add item 3
        var updatedItems = new SaveBudgetItemsRequest([
            new BudgetItemInput(existingItemId, 1, "活動A-更新", "項目A-更新", 1500, null, null, null),
            new BudgetItemInput(null, 3, "活動C", "項目C", 3000, null, null, null),
        ]);

        var result = await _sut.SaveDepartmentItemsAsync(2025, deptId, updatedItems);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.ActivityName == "活動A-更新" && i.Amount == 1500);
        result.Should().Contain(i => i.ActivityName == "活動C" && i.Amount == 3000);
        result.Should().NotContain(i => i.ActivityName == "活動B");
    }

    // ── RecalculateAllocations ──────────────────────

    [Fact]
    public async Task RecalculateAllocations_ProportionalDistribution()
    {
        // Arrange — 2 departments with items totaling 60k and 40k
        var dept1Id = await SeedDepartmentAsync("體育組");
        var dept2Id = await SeedDepartmentAsync("競技組");

        await _sut.GetOverviewAsync(2025); // auto-init

        await _sut.SaveDepartmentItemsAsync(2025, dept1Id, new SaveBudgetItemsRequest([
            new BudgetItemInput(null, 1, "活動A", "項目A", 60000, null, null, null),
        ]));
        await _sut.SaveDepartmentItemsAsync(2025, dept2Id, new SaveBudgetItemsRequest([
            new BudgetItemInput(null, 1, "活動B", "項目B", 40000, null, null, null),
        ]));

        // Act — grant 80k
        var result = await _sut.UpdateGrantedBudgetAsync(2025, 80000);

        // Assert
        var dept1 = result.Departments.First(d => d.DepartmentName == "體育組");
        var dept2 = result.Departments.First(d => d.DepartmentName == "競技組");
        dept1.AllocatedAmount.Should().Be(48000);
        dept2.AllocatedAmount.Should().Be(32000);
    }

    [Fact]
    public async Task RecalculateAllocations_ZeroTotal_AllZero()
    {
        // Arrange — departments with no items
        await SeedDepartmentAsync("體育組");
        await SeedDepartmentAsync("競技組");
        await _sut.GetOverviewAsync(2025);

        // Act — grant 100k but no items exist
        var result = await _sut.UpdateGrantedBudgetAsync(2025, 100000);

        // Assert
        result.Departments.Should().AllSatisfy(d =>
            d.AllocatedAmount.Should().Be(0));
    }

    // ── Helpers ─────────────────────────────────────

    private async Task<Guid> SeedDepartmentAsync(string name)
    {
        var dept = new SportsDepartment
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.SportsDepartments.Add(dept);
        await _db.SaveChangesAsync();
        return dept.Id;
    }

    private async Task SeedBudgetAsync(int year, BudgetStatus status)
    {
        var now = DateTime.UtcNow;
        _db.AnnualBudgets.Add(new AnnualBudget
        {
            Id = Guid.NewGuid(),
            Year = year,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
