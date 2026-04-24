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

        var todoItem = Entity<TodoItem>(model);
        todoItem.FindProperty(nameof(TodoItem.Title))!.GetMaxLength().Should().Be(500);
        todoItem.FindProperty(nameof(TodoItem.Description))!.GetMaxLength().Should().Be(2000);
        ShouldHaveIndex(todoItem, isUnique: false, nameof(TodoItem.UserId), nameof(TodoItem.Status));
        ShouldHaveIndex(todoItem, isUnique: false, nameof(TodoItem.UserId), nameof(TodoItem.DueDate));
        ShouldHaveIndex(todoItem, isUnique: false, nameof(TodoItem.UserId), nameof(TodoItem.ProjectId));
        ShouldHaveIndex(todoItem, isUnique: false, nameof(TodoItem.UserId), nameof(TodoItem.DeletedAt));

        var todoCategory = Entity<TodoCategory>(model);
        ShouldHaveIndex(todoCategory, isUnique: true, nameof(TodoCategory.UserId), nameof(TodoCategory.Name));

        var articleBookmark = Entity<ArticleBookmark>(model);
        articleBookmark.FindProperty(nameof(ArticleBookmark.Tags))!.GetColumnType().Should().Be("text[]");
        articleBookmark.FindProperty(nameof(ArticleBookmark.Tags))!.GetDefaultValueSql()
            .Should().Be("ARRAY[]::text[]");

        var urlIndex = ShouldHaveIndex(
            articleBookmark,
            isUnique: true,
            nameof(ArticleBookmark.UserId),
            nameof(ArticleBookmark.Url));
        urlIndex.GetFilter().Should().Be("\"SourceType\" = 1 AND \"Url\" IS NOT NULL");

        var sourceIndex = ShouldHaveIndex(
            articleBookmark,
            isUnique: true,
            nameof(ArticleBookmark.UserId),
            nameof(ArticleBookmark.SourceId));
        sourceIndex.GetFilter().Should().Be("\"SourceType\" = 2 AND \"SourceId\" IS NOT NULL");

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
