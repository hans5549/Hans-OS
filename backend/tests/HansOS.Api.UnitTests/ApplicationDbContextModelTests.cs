using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HansOS.Api.UnitTests;

public class ApplicationDbContextModelTests
{
    [Fact]
    public void RuntimeModel_AppliesSeparatedEntityConfigurations()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=hansos_model_test;Username=test;Password=test")
            .Options;

        using var db = new ApplicationDbContext(options);
        var model = db.Model;

        var financeTask = Entity<FinanceTask>(model);
        ShouldHaveIndex(financeTask, isUnique: false, nameof(FinanceTask.Status));
        ShouldHaveIndex(financeTask, isUnique: false, nameof(FinanceTask.DueDate));
        ShouldHaveIndex(financeTask, isUnique: false, nameof(FinanceTask.CreatedAt));
    }

    private static IReadOnlyEntityType Entity<TEntity>(IModel model)
    {
        return model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not in the EF model");
    }

    private static IReadOnlyIndex ShouldHaveIndex(
        IReadOnlyEntityType entityType,
        bool isUnique,
        params string[] propertyNames)
    {
        var index = entityType.GetIndexes().SingleOrDefault(i =>
            i.IsUnique == isUnique
            && i.Properties.Select(p => p.Name).SequenceEqual(propertyNames));

        Assert.NotNull(index);

        return index!;
    }
}
