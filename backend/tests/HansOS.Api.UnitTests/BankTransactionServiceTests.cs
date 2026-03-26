using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Models.BankTransactions;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HansOS.Api.UnitTests;

public class BankTransactionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly BankTransactionService _sut;

    public BankTransactionServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(dbOptions);
        _sut = new BankTransactionService(_db);
    }

    // ── GetTransactionsAsync ────────────────────────

    [Fact]
    public async Task GetTransactions_ValidParams_ReturnsList()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 3, 1), 5000, "收入A");
        await SeedTransactionAsync("上海銀行", TransactionType.Expense, new DateOnly(2026, 3, 15), 2000, "支出A");

        var result = await _sut.GetTransactionsAsync("上海銀行", 2026, 3);

        result.Should().HaveCount(2);
        result[0].Description.Should().Be("收入A");
        result[1].Description.Should().Be("支出A");
    }

    [Fact]
    public async Task GetTransactions_NoMatchingBank_ReturnsEmptyList()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 3, 1), 5000, "收入A");

        var result = await _sut.GetTransactionsAsync("不存在銀行", 2026, 3);

        result.Should().BeEmpty();
    }

    // ── GetPeriodSummaryAsync ───────────────────────

    [Fact]
    public async Task GetPeriodSummary_ValidParams_ReturnsCorrectSummary()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 3, 1), 10000, "收入", fee: 0);
        await SeedTransactionAsync("上海銀行", TransactionType.Expense, new DateOnly(2026, 3, 15), 3000, "支出", fee: 15);

        var result = await _sut.GetPeriodSummaryAsync("上海銀行", 2026, 3);

        result.TotalIncome.Should().Be(10000m);
        result.TotalExpense.Should().Be(3015m);
        result.OpeningBalance.Should().Be(0m);
        result.ClosingBalance.Should().Be(6985m);
    }

    [Fact]
    public async Task GetPeriodSummary_NoTransactions_ReturnsZeroSummary()
    {
        var result = await _sut.GetPeriodSummaryAsync("上海銀行", 2026, 3);

        result.TotalIncome.Should().Be(0);
        result.TotalExpense.Should().Be(0);
        result.OpeningBalance.Should().Be(0);
        result.ClosingBalance.Should().Be(0);
    }

    // ── CreateTransactionAsync ──────────────────────

    [Fact]
    public async Task CreateTransaction_ValidData_ReturnsCreatedTransaction()
    {
        var request = new CreateBankTransactionRequest(
            TransactionType: TransactionType.Income,
            TransactionDate: new DateOnly(2026, 4, 1),
            Description: "會費收入",
            DepartmentId: null,
            RequestingUnit: "教務處",
            Amount: 5000.50m,
            Fee: 15,
            HasReceipt: true,
            ReceiptMailed: false);

        var result = await _sut.CreateTransactionAsync("上海銀行", request);

        result.Id.Should().NotBeEmpty();
        result.BankName.Should().Be("上海銀行");
        result.Description.Should().Be("會費收入");
        result.Amount.Should().Be(5000.50m);
        result.Fee.Should().Be(15m);
        result.HasReceipt.Should().BeTrue();
        result.RequestingUnit.Should().Be("教務處");

        // 驗證已存入資料庫
        var saved = await _db.BankTransactions.FindAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTransaction_InvalidDepartment_ThrowsError()
    {
        var request = new CreateBankTransactionRequest(
            TransactionType: TransactionType.Income,
            TransactionDate: new DateOnly(2026, 4, 1),
            Description: "測試",
            DepartmentId: Guid.NewGuid(),
            RequestingUnit: null,
            Amount: 100);

        var act = () => _sut.CreateTransactionAsync("上海銀行", request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*部門不存在*");
    }

    // ── UpdateTransactionAsync ──────────────────────

    [Fact]
    public async Task UpdateTransaction_ValidData_UpdatesSuccessfully()
    {
        var id = await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 5, 1), 1000, "原始");

        var request = new UpdateBankTransactionRequest(
            TransactionType: TransactionType.Expense,
            TransactionDate: new DateOnly(2026, 5, 2),
            Description: "已更新",
            DepartmentId: null,
            RequestingUnit: "總務處",
            Amount: 2000,
            Fee: 10,
            HasReceipt: true,
            ReceiptMailed: true);

        await _sut.UpdateTransactionAsync(id, request);

        var updated = await _db.BankTransactions.FindAsync(id);
        updated!.Description.Should().Be("已更新");
        updated.Amount.Should().Be(2000m);
        updated.TransactionType.Should().Be(TransactionType.Expense);
        updated.RequestingUnit.Should().Be("總務處");
    }

    [Fact]
    public async Task UpdateTransaction_NotFound_ThrowsError()
    {
        var request = new UpdateBankTransactionRequest(
            TransactionType: TransactionType.Income,
            TransactionDate: new DateOnly(2026, 5, 1),
            Description: "不存在",
            DepartmentId: null,
            RequestingUnit: null,
            Amount: 100);

        var act = () => _sut.UpdateTransactionAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── DeleteTransactionAsync ──────────────────────

    [Fact]
    public async Task DeleteTransaction_ValidId_DeletesSuccessfully()
    {
        var id = await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 6, 1), 500, "待刪除");

        await _sut.DeleteTransactionAsync(id);

        var deleted = await _db.BankTransactions.FindAsync(id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTransaction_NotFound_ThrowsError()
    {
        var act = () => _sut.DeleteTransactionAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── ExportToExcelAsync ──────────────────────────

    [Fact]
    public async Task ExportToExcel_ValidParams_ReturnsBytes()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2026, 7, 1), 10000, "收入");

        var result = await _sut.ExportToExcelAsync("上海銀行", 2026, 7);

        result.Should().NotBeEmpty();
        // XLSX magic bytes: PK (50 4B)
        result[0].Should().Be(0x50);
        result[1].Should().Be(0x4B);
    }

    [Fact]
    public async Task ExportToExcel_NoTransactions_ReturnsValidExcel()
    {
        var result = await _sut.ExportToExcelAsync("上海銀行", 2026, 12);

        result.Should().NotBeEmpty();
        result[0].Should().Be(0x50);
        result[1].Should().Be(0x4B);
    }

    // ── Helpers ─────────────────────────────────────

    private async Task<Guid> SeedTransactionAsync(
        string bankName, TransactionType type, DateOnly date, decimal amount, string description,
        decimal fee = 0)
    {
        var entity = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankName = bankName,
            TransactionType = type,
            TransactionDate = date,
            Description = description,
            Amount = amount,
            Fee = fee,
            HasReceipt = false,
            ReceiptMailed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.BankTransactions.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
