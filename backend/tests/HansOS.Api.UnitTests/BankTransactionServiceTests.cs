using ClosedXML.Excel;

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
            Amount: 5000,
            Fee: 15);

        var result = await _sut.CreateTransactionAsync("上海銀行", request);

        result.Id.Should().NotBeEmpty();
        result.BankName.Should().Be("上海銀行");
        result.Description.Should().Be("會費收入");
        result.Amount.Should().Be(5000m);
        result.Fee.Should().Be(15m);
        // 收入不追蹤收據
        result.ReceiptCollected.Should().BeFalse();
        result.ReceiptMailed.Should().BeFalse();

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
            Amount: 2000,
            Fee: 10,
            ReceiptCollected: true,
            ReceiptMailed: true);

        await _sut.UpdateTransactionAsync(id, request);

        var updated = await _db.BankTransactions.FindAsync(id);
        updated!.Description.Should().Be("已更新");
        updated.Amount.Should().Be(2000m);
        updated.TransactionType.Should().Be(TransactionType.Expense);
    }

    [Fact]
    public async Task UpdateTransaction_NotFound_ThrowsError()
    {
        var request = new UpdateBankTransactionRequest(
            TransactionType: TransactionType.Income,
            TransactionDate: new DateOnly(2026, 5, 1),
            Description: "不存在",
            DepartmentId: null,
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

    // ── GetPeriodSummary Edge Cases — TODO 4 ────────────────

    /// <summary>只有收入交易時，支出應為零</summary>
    [Fact]
    public async Task GetPeriodSummary_OnlyIncome_ExpenseIsZero()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2027, 1, 1), 5000, "收入A");
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2027, 1, 15), 3000, "收入B");

        var result = await _sut.GetPeriodSummaryAsync("上海銀行", 2027, 1);

        result.TotalIncome.Should().Be(8000m);
        result.TotalExpense.Should().Be(0m);
        result.ClosingBalance.Should().Be(result.OpeningBalance + 8000m);
    }

    /// <summary>只有支出交易時，收入應為零</summary>
    [Fact]
    public async Task GetPeriodSummary_OnlyExpense_IncomeIsZero()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Expense, new DateOnly(2027, 2, 1), 2000, "支出A");
        await SeedTransactionAsync("上海銀行", TransactionType.Expense, new DateOnly(2027, 2, 15), 1000, "支出B");

        var result = await _sut.GetPeriodSummaryAsync("上海銀行", 2027, 2);

        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(3000m);
        result.ClosingBalance.Should().Be(result.OpeningBalance - 3000m);
    }

    // ── ExportToExcel Edge Cases — TODO 3 ───────────────────

    /// <summary>只有收入交易時產出有效的 Excel，支出欄應為 0</summary>
    [Fact]
    public async Task ExportToExcel_OnlyIncome_ProducesValidExcel()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Income, new DateOnly(2027, 3, 1), 10000, "收入");

        var result = await _sut.ExportToExcelAsync("上海銀行", 2027, 3);

        result.Should().NotBeEmpty();
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.Cell(5, 1).GetString().Should().Be("日期");
        ws.Cell(6, 4).GetDouble().Should().Be(10000);
        ws.Cell(6, 5).GetDouble().Should().Be(0);
    }

    /// <summary>只有支出交易時產出有效的 Excel，收入欄應為 0</summary>
    [Fact]
    public async Task ExportToExcel_OnlyExpense_ProducesValidExcel()
    {
        await SeedTransactionAsync("上海銀行", TransactionType.Expense, new DateOnly(2027, 4, 1), 3000, "支出");

        var result = await _sut.ExportToExcelAsync("上海銀行", 2027, 4);

        result.Should().NotBeEmpty();
        using var stream = new MemoryStream(result);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();

        ws.Cell(5, 1).GetString().Should().Be("日期");
        ws.Cell(6, 4).GetDouble().Should().Be(0);
        ws.Cell(6, 5).GetDouble().Should().Be(3000);
    }

    // ── CreateTransaction — Receipt for Expense Only ───────────────

    /// <summary>建立支出交易時 ReceiptCollected=true 應正確儲存至資料庫</summary>
    [Fact]
    public async Task CreateTransaction_ExpenseWithReceiptCollected_MapsCorrectly()
    {
        var request = new CreateBankTransactionRequest(
            TransactionType: TransactionType.Expense,
            TransactionDate: new DateOnly(2028, 1, 1),
            Description: "收據已領取",
            DepartmentId: null,
            Amount: 1000,
            Fee: 0,
            ReceiptCollected: true,
            ReceiptMailed: false);

        var result = await _sut.CreateTransactionAsync("上海銀行", request);

        result.ReceiptCollected.Should().BeTrue();

        var saved = await _db.BankTransactions.FindAsync(result.Id);
        saved!.ReceiptCollected.Should().BeTrue();
    }

    /// <summary>建立收入交易時，即使傳入 ReceiptCollected=true 也應被忽略</summary>
    [Fact]
    public async Task CreateTransaction_IncomeIgnoresReceiptFlags()
    {
        var request = new CreateBankTransactionRequest(
            TransactionType: TransactionType.Income,
            TransactionDate: new DateOnly(2028, 1, 1),
            Description: "收入不追蹤收據",
            DepartmentId: null,
            Amount: 1000,
            Fee: 0,
            ReceiptCollected: true,
            ReceiptMailed: true);

        var result = await _sut.CreateTransactionAsync("上海銀行", request);

        result.ReceiptCollected.Should().BeFalse();
        result.ReceiptMailed.Should().BeFalse();
    }

    // ── GetReceiptTrackingAsync ──────────────────────────────

    /// <summary>支出交易未領取收據應出現在追蹤清單</summary>
    [Fact]
    public async Task GetReceiptTracking_ReturnsUnprocessedReceipts()
    {
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Expense,
            new DateOnly(2030, 1, 10), 1000, "未處理收據",
            receiptCollected: false, receiptMailed: false);

        var result = await _sut.GetReceiptTrackingAsync(2030, 1);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Description == "未處理收據");
    }

    /// <summary>收據已全部處理完成（Collected=true, Mailed=true）不應出現在追蹤清單</summary>
    [Fact]
    public async Task GetReceiptTracking_ExcludesFullyProcessed()
    {
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Expense,
            new DateOnly(2030, 2, 10), 1000, "已處理完成",
            receiptCollected: true, receiptMailed: true);

        var result = await _sut.GetReceiptTrackingAsync(2030, 2);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>支出交易 ReceiptCollected=false 應出現（尚未領取）</summary>
    [Fact]
    public async Task GetReceiptTracking_IncludesNotCollected()
    {
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Expense,
            new DateOnly(2030, 3, 10), 2000, "未領取收據",
            receiptCollected: false, receiptMailed: true);

        var result = await _sut.GetReceiptTrackingAsync(2030, 3);

        result.TotalCount.Should().Be(1);
        result.NotCollectedCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Description == "未領取收據");
    }

    /// <summary>支出交易 ReceiptMailed=false 應出現（尚未寄送）</summary>
    [Fact]
    public async Task GetReceiptTracking_IncludesNotMailed()
    {
        await SeedTransactionWithReceiptAsync("合作金庫", TransactionType.Expense,
            new DateOnly(2030, 4, 10), 3000, "未寄送收據",
            receiptCollected: true, receiptMailed: false);

        var result = await _sut.GetReceiptTrackingAsync(2030, 4);

        result.TotalCount.Should().Be(1);
        result.NotMailedCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.Description == "未寄送收據");
    }

    /// <summary>收入交易不論收據狀態，都不應出現在追蹤清單</summary>
    [Fact]
    public async Task GetReceiptTracking_ExcludesIncomeTransactions()
    {
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Income,
            new DateOnly(2030, 6, 10), 5000, "收入交易",
            receiptCollected: false, receiptMailed: false);

        var result = await _sut.GetReceiptTrackingAsync(2030, 6);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>混合狀態的交易，驗證 TotalCount/NotCollectedCount/NotMailedCount 正確計算</summary>
    [Fact]
    public async Task GetReceiptTracking_CountsCorrectly()
    {
        // 支出：未領取、未寄送 → 計入 total, notCollected, notMailed
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Expense,
            new DateOnly(2030, 5, 1), 1000, "狀態A",
            receiptCollected: false, receiptMailed: false);

        // 支出：已領取、未寄送 → 計入 total, notMailed
        await SeedTransactionWithReceiptAsync("上海銀行", TransactionType.Expense,
            new DateOnly(2030, 5, 5), 2000, "狀態B",
            receiptCollected: true, receiptMailed: false);

        // 支出：未領取、已寄送 → 計入 total, notCollected
        await SeedTransactionWithReceiptAsync("合作金庫", TransactionType.Expense,
            new DateOnly(2030, 5, 10), 3000, "狀態C",
            receiptCollected: false, receiptMailed: true);

        // 支出：已全部處理 → 不計入任何
        await SeedTransactionWithReceiptAsync("合作金庫", TransactionType.Expense,
            new DateOnly(2030, 5, 15), 4000, "狀態D-已完成",
            receiptCollected: true, receiptMailed: true);

        // 收入：不論狀態都不計入
        await SeedTransactionAsync("上海銀行", TransactionType.Income,
            new DateOnly(2030, 5, 20), 5000, "收入交易");

        var result = await _sut.GetReceiptTrackingAsync(2030, 5);

        result.TotalCount.Should().Be(3);
        result.NotCollectedCount.Should().Be(2);
        result.NotMailedCount.Should().Be(2);
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
            ReceiptCollected = false,
            ReceiptMailed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.BankTransactions.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task<Guid> SeedTransactionWithReceiptAsync(
        string bankName, TransactionType type, DateOnly date, decimal amount, string description,
        bool receiptCollected, bool receiptMailed, decimal fee = 0)
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
            ReceiptCollected = receiptCollected,
            ReceiptMailed = receiptMailed,
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
