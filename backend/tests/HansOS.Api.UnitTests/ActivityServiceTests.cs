using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.Activities;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.UnitTests;

public class ActivityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ActivityService _sut;
    private readonly Guid _departmentId = Guid.NewGuid();

    public ActivityServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(dbOptions);
        _sut = new ActivityService(_db);

        SeedDepartment();
    }

    // ── GetListAsync ────────────────────────────────

    [Fact]
    public async Task GetList_ReturnsFilteredAndOrderedResults()
    {
        await SeedActivityAsync("三月活動", 2026, 3);
        await SeedActivityAsync("五月活動", 2026, 5);

        var result = await _sut.GetListAsync(2026, month: 3, departmentId: null);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("三月活動");
        result[0].Month.Should().Be(3);
    }

    // ── GetDetailAsync ──────────────────────────────

    [Fact]
    public async Task GetDetail_ReturnsCorrectTotalAmount()
    {
        var activityId = await SeedActivityWithExpensesAsync("活動A", 2026, 6,
            [1000m, 2500m, 500m]);

        var result = await _sut.GetDetailAsync(activityId);

        result.TotalAmount.Should().Be(4000m);
        result.Name.Should().Be("活動A");
    }

    [Fact]
    public async Task GetDetail_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.GetDetailAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*活動不存在*");
    }

    // ── CreateAsync ─────────────────────────────────

    [Fact]
    public async Task Create_ValidData_PersistsToDatabase()
    {
        var request = new CreateActivityRequest(
            DepartmentId: _departmentId,
            Year: 2026,
            Month: 4,
            Name: "新活動",
            Description: "測試描述",
            Groups: null,
            Expenses:
            [
                new ActivityExpenseInput(null, "開銷一", 3000m, null, 1, null),
                new ActivityExpenseInput(null, "開銷二", 1500m, "備註", 2, null),
            ]);

        var result = await _sut.CreateAsync(request);

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("新活動");
        result.TotalAmount.Should().Be(4500m);

        var saved = await _db.Activities.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Create_InvalidDepartment_ThrowsArgumentException()
    {
        var request = new CreateActivityRequest(
            DepartmentId: Guid.NewGuid(),
            Year: 2026,
            Month: 4,
            Name: "測試",
            Description: null,
            Groups: null,
            Expenses: null);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*部門不存在*");
    }

    [Fact]
    public async Task Create_BudgetItemFromDifferentYear_ThrowsArgumentException()
    {
        var budgetItemId = await SeedBudgetItemAsync(_departmentId, 2025);
        var request = new CreateActivityRequest(
            DepartmentId: _departmentId,
            Year: 2026,
            Month: 4,
            Name: "年度不符",
            Description: null,
            Groups: null,
            Expenses:
            [
                new ActivityExpenseInput(null, "開銷一", 3000m, null, 1, budgetItemId),
            ]);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*相同部門與年度*");
    }

    [Fact]
    public async Task Create_BudgetItemFromDifferentDepartment_ThrowsArgumentException()
    {
        var otherDepartmentId = Guid.NewGuid();
        await SeedDepartmentAsync(otherDepartmentId, "其他部門");
        var budgetItemId = await SeedBudgetItemAsync(otherDepartmentId, 2026);
        var request = new CreateActivityRequest(
            DepartmentId: _departmentId,
            Year: 2026,
            Month: 4,
            Name: "部門不符",
            Description: null,
            Groups: null,
            Expenses:
            [
                new ActivityExpenseInput(null, "開銷一", 3000m, null, 1, budgetItemId),
            ]);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*相同部門與年度*");
    }

    // ── UpdateAsync ─────────────────────────────────

    [Fact]
    public async Task Update_ValidData_ReplacesExpenses()
    {
        var activityId = await SeedActivityWithExpensesAsync("原始活動", 2026, 7,
            [1000m, 2000m]);

        var request = new UpdateActivityRequest(
            Name: "已更新活動",
            Description: "新描述",
            Month: null,
            Groups: null,
            Expenses:
            [
                new ActivityExpenseInput(null, "唯一開銷", 5000m, null, 1, null),
            ]);

        var result = await _sut.UpdateAsync(activityId, request);

        result.Name.Should().Be("已更新活動");
        result.TotalAmount.Should().Be(5000m);

        var expenses = await _db.ActivityExpenses
            .Where(e => e.ActivityId == activityId)
            .ToListAsync();
        expenses.Should().HaveCount(1);
        expenses[0].Description.Should().Be("唯一開銷");
    }

    [Fact]
    public async Task Update_NotFound_ThrowsKeyNotFoundException()
    {
        var request = new UpdateActivityRequest(
            Name: "不存在",
            Description: null,
            Month: null,
            Groups: null,
            Expenses: null);

        var act = () => _sut.UpdateAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteAsync ─────────────────────────────────

    [Fact]
    public async Task Delete_ValidId_RemovesEntity()
    {
        var activityId = await SeedActivityAsync("待刪除", 2026, 8);

        await _sut.DeleteAsync(activityId);

        var deleted = await _db.Activities.FindAsync(activityId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetMonthSummariesAsync ──────────────────────

    [Fact]
    public async Task GetMonthSummaries_AggregatesCorrectly()
    {
        await SeedActivityWithExpensesAsync("六月A", 2026, 6, [1000m, 500m]);
        await SeedActivityWithExpensesAsync("六月B", 2026, 6, [2000m]);
        await SeedActivityWithExpensesAsync("七月A", 2026, 7, [3000m]);

        var result = await _sut.GetMonthSummariesAsync(2026, departmentId: null);

        result.Should().HaveCount(2);

        var juneSummary = result.First(s => s.Month == 6);
        juneSummary.ActivityCount.Should().Be(2);
        juneSummary.TotalAmount.Should().Be(3500m);

        var julySummary = result.First(s => s.Month == 7);
        julySummary.ActivityCount.Should().Be(1);
        julySummary.TotalAmount.Should().Be(3000m);
    }

    // ── Helpers ─────────────────────────────────────

    private void SeedDepartment()
    {
        SeedDepartmentAsync(_departmentId, "測試部門").GetAwaiter().GetResult();
    }

    private async Task SeedDepartmentAsync(Guid departmentId, string name)
    {
        var now = DateTime.UtcNow;
        _db.SportsDepartments.Add(new SportsDepartment
        {
            Id = departmentId,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await _db.SaveChangesAsync();
    }

    private async Task<Guid> SeedBudgetItemAsync(Guid departmentId, int year)
    {
        var now = DateTime.UtcNow;
        var annualBudget = new AnnualBudget
        {
            Id = Guid.NewGuid(),
            Year = year,
            Status = BudgetStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var departmentBudget = new DepartmentBudget
        {
            Id = Guid.NewGuid(),
            AnnualBudgetId = annualBudget.Id,
            DepartmentId = departmentId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var budgetItem = new BudgetItem
        {
            Id = Guid.NewGuid(),
            DepartmentBudgetId = departmentBudget.Id,
            Sequence = 1,
            ActivityName = "測試活動",
            ContentItem = "測試項目",
            Amount = 1000m,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.AnnualBudgets.Add(annualBudget);
        _db.DepartmentBudgets.Add(departmentBudget);
        _db.BudgetItems.Add(budgetItem);
        _db.SaveChanges();
        return budgetItem.Id;
    }

    private async Task<Guid> SeedActivityAsync(string name, int year, int month)
    {
        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            DepartmentId = _departmentId,
            Year = year,
            Month = month,
            Name = name,
            Sequence = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity.Id;
    }

    private async Task<Guid> SeedActivityWithExpensesAsync(
        string name, int year, int month, decimal[] amounts)
    {
        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            DepartmentId = _departmentId,
            Year = year,
            Month = month,
            Name = name,
            Sequence = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        for (var i = 0; i < amounts.Length; i++)
        {
            activity.Expenses.Add(new ActivityExpense
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
                Description = $"開銷{i + 1}",
                Amount = amounts[i],
                Sequence = i + 1,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        return activity.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
