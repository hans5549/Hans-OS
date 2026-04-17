using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HansOS.Api.UnitTests;

/// <summary>
/// BankTransactionImportService 使用 ExecuteDeleteAsync 和 Database.BeginTransactionAsync，
/// 這些功能在 EF Core InMemory Provider 中不支援，因此該 Service 的測試
/// 以整合測試 (BankTransactionControllerTests) 覆蓋為主。
/// </summary>
public class BankTransactionImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public BankTransactionImportServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(dbOptions);
    }

    [Fact]
    public void ServiceCanBeConstructed()
    {
        var sut = new BankTransactionImportService(_db, Substitute.For<ILogger<BankTransactionImportService>>());

        Assert.NotNull(sut);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
