using FluentAssertions;
using HansOS.Api.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace HansOS.Api.IntegrationTests;

public class MenuMigrationTests
{
    private const string AdminRoleId = "a1b2c3d4-0000-0000-0000-000000000001";
    private const string DashboardMenuId = "d1e2f3a4-0000-0000-0000-000000000001";
    private const string AnalyticsMenuId = "d1e2f3a4-0000-0000-0000-000000000002";
    private const string WorkspaceMenuId = "d1e2f3a4-0000-0000-0000-000000000003";
    private const string TodoMenuId = "d1e2f3a4-0000-0000-0000-000000000010";
    private const string SystemDesignParentId = "c0000001-0000-0000-0000-000000000001";
    private const string FundamentalsId = "c0000002-0000-0000-0000-000000000001";
    private const string NetworkingEssentialsId = "c0000002-0000-0000-0000-000000000101";
    private const string RealWorldAppsId = "c0000002-0000-0000-0000-000000000005";
    private const string QrCodeGeneratorId = "c0000001-0000-0000-0000-000000000002";

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

    [Fact]
    public void RemoveDashboardMenusAndResetHomePath_Up_EmitsCleanupSql()
    {
        var migration = new RemoveDashboardMenusAndResetHomePathAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyUp(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"RoleMenus\"") &&
            sql.Contains($"'{TodoMenuId}'") &&
            sql.Contains($"'{AnalyticsMenuId}'") &&
            sql.Contains($"'{WorkspaceMenuId}'") &&
            sql.Contains($"'{DashboardMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"Menus\"") &&
            sql.Contains($"'{TodoMenuId}'") &&
            sql.Contains($"'{AnalyticsMenuId}'") &&
            sql.Contains($"'{WorkspaceMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"Menus\"") &&
            sql.Contains($"'{DashboardMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"AspNetUsers\"") &&
            sql.Contains("SET \"HomePath\" = '/index'") &&
            sql.Contains("WHERE \"HomePath\" IN ('/dashboard', '/analytics', '/workspace', '/todo')"));
    }

    [Fact]
    public void RemoveDashboardMenusAndResetHomePath_Down_RecreatesMenuTreeAndHomePath()
    {
        var migration = new RemoveDashboardMenusAndResetHomePathAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyDown(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("'Dashboard'") &&
            sql.Contains("'/dashboard'") &&
            sql.Contains("'/analytics'") &&
            sql.Contains($"'{DashboardMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("'Analytics'") &&
            sql.Contains($"'{AnalyticsMenuId}'") &&
            sql.Contains("'/dashboard/analytics/index'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("'Workspace'") &&
            sql.Contains($"'{WorkspaceMenuId}'") &&
            sql.Contains("'/dashboard/workspace/index'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("'Todo'") &&
            sql.Contains($"'{TodoMenuId}'") &&
            sql.Contains("'[\"admin\"]'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"RoleMenus\"") &&
            sql.Contains($"'{AdminRoleId}'") &&
            sql.Contains($"'{DashboardMenuId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"AspNetUsers\"") &&
            sql.Contains("SET \"HomePath\" = '/analytics'") &&
            sql.Contains("WHERE \"HomePath\" = '/index'"));
    }

    [Fact]
    public void ReorganizeSystemDesignMenus_Up_EmitsReorganizedMenuSql()
    {
        var migration = new ReorganizeSystemDesignMenusAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyUp(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"Menus\"") &&
            sql.Contains($"'{FundamentalsId}'") &&
            sql.Contains("'Fundamentals'") &&
            sql.Contains("'/system-design/fundamentals'") &&
            sql.Contains("'/system-design/fundamentals/networking-essentials'") &&
            sql.Contains("'基本觀念'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"Menus\"") &&
            sql.Contains($"'{NetworkingEssentialsId}'") &&
            sql.Contains("'NetworkingEssentials'") &&
            sql.Contains("'/system-design/fundamentals/networking-essentials/index'") &&
            sql.Contains("'Networking Essentials | 網路基本原理'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"Menus\"") &&
            sql.Contains($"'{QrCodeGeneratorId}'") &&
            sql.Contains($"'{RealWorldAppsId}'") &&
            sql.Contains("'/system-design/real-world-apps/qr-code-generator'") &&
            sql.Contains("'/system-design/real-world-apps/qr-code-generator/index'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"Menus\"") &&
            sql.Contains($"'{SystemDesignParentId}'") &&
            sql.Contains("'/system-design/fundamentals/networking-essentials'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"AspNetUsers\"") &&
            sql.Contains("SET \"HomePath\" = '/system-design/real-world-apps/qr-code-generator'") &&
            sql.Contains("WHERE \"HomePath\" = '/system-design/qr-code-generator'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("INSERT INTO \"RoleMenus\"") &&
            sql.Contains($"'{AdminRoleId}'") &&
            sql.Contains($"'{FundamentalsId}'") &&
            sql.Contains($"'{NetworkingEssentialsId}'"));
    }

    [Fact]
    public void ReorganizeSystemDesignMenus_Down_EmitsRollbackSql()
    {
        var migration = new ReorganizeSystemDesignMenusAccessor();
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migration.ApplyDown(migrationBuilder);

        var sqlOperations = migrationBuilder.Operations
            .OfType<SqlOperation>()
            .Select(operation => operation.Sql)
            .ToArray();

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"RoleMenus\"") &&
            sql.Contains($"'{FundamentalsId}'") &&
            sql.Contains($"'{NetworkingEssentialsId}'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"Menus\"") &&
            sql.Contains($"'{QrCodeGeneratorId}'") &&
            sql.Contains($"'{SystemDesignParentId}'") &&
            sql.Contains("'/system-design/qr-code-generator'") &&
            sql.Contains("'/system-design/qr-code-generator/index'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"Menus\"") &&
            sql.Contains($"'{SystemDesignParentId}'") &&
            sql.Contains("'/system-design/qr-code-generator'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("UPDATE \"AspNetUsers\"") &&
            sql.Contains("SET \"HomePath\" = '/system-design/qr-code-generator'") &&
            sql.Contains("WHERE \"HomePath\" = '/system-design/real-world-apps/qr-code-generator'"));

        sqlOperations.Should().Contain(sql =>
            sql.Contains("DELETE FROM \"Menus\"") &&
            sql.Contains($"'{FundamentalsId}'") &&
            sql.Contains($"'{NetworkingEssentialsId}'") &&
            sql.Contains($"'{RealWorldAppsId}'"));
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

    private sealed class RemoveDashboardMenusAndResetHomePathAccessor : RemoveDashboardMenusAndResetHomePath
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

    private sealed class ReorganizeSystemDesignMenusAccessor : ReorganizeSystemDesignMenus
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
