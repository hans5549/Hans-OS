using FluentAssertions;
using HansOS.Api.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace HansOS.Api.IntegrationTests;

public class MenuMigrationTests
{
    private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
    private const string DashboardMenuId = "d1e2f3a4-0000-0000-0000-000000000001";
    private const string TodoMenuId = "d1e2f3a4-0000-0000-0000-000000000010";

    [Fact]
    public void AddTodoMenuItem_Up_EmitsTodoMenuSeedSql()
    {
        var migration = new AddTodoMenuItemAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyUp(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("'Todo'") &&
            sql.Contains($"'{TodoMenuId}'") &&
            sql.Contains("'/todo'") &&
            sql.Contains("'/dashboard/todo/index'") &&
            sql.Contains("'page.dashboard.todo'") &&
            sql.Contains("'[\"admin\"]'") &&
            sql.Contains($"'{DashboardMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"RoleMenus\"") &&
            sql.Contains($"'{AdminRoleId}'") &&
            sql.Contains($"'{TodoMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"Menus\"") &&
            sql.Contains("\"Authority\" = '[\"admin\"]'") &&
            sql.Contains("AND (\"Authority\" IS NULL OR \"Authority\" = '')") &&
            sql.Contains($"'{TodoMenuId}'"));
    }

    [Fact]
    public void AddTodoMenuItem_Down_EmitsTodoMenuRollbackSql()
    {
        var migration = new AddTodoMenuItemAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyDown(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"RoleMenus\"") &&
            sql.Contains($"'{TodoMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"Menus\"") &&
            sql.Contains($"'{TodoMenuId}'"));
    }

    private sealed class AddTodoMenuItemAccessor : AddTodoMenuItem
    {
        public void ApplyUp(MigrationBuilder migrationBuilder)
        {
            Up(migrationBuilder);
        }

        public void ApplyDown(MigrationBuilder migrationBuilder)
        {
            Down(migrationBuilder);
        }
    }
}
