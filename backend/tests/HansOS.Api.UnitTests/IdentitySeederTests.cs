using FluentAssertions;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Data.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HansOS.Api.UnitTests;

public class IdentitySeederTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public IdentitySeederTests()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesAdminRoleAndUser()
    {
        await IdentitySeeder.SeedAsync(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roleExists = await roleManager.RoleExistsAsync("admin");
        roleExists.Should().BeTrue();

        var user = await userManager.FindByNameAsync("hans");
        Assert.NotNull(user);
        user!.RealName.Should().Be("Hans");
        user.Email.Should().Be("hans@hans-os.dev");
        user.IsActive.Should().BeTrue();

        var inAdmin = await userManager.IsInRoleAsync(user, "admin");
        inAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_RunTwice_IsIdempotent()
    {
        await IdentitySeeder.SeedAsync(_serviceProvider);
        await IdentitySeeder.SeedAsync(_serviceProvider);

        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var users = userManager.Users.Where(u => u.UserName == "hans").ToList();
        users.Should().HaveCount(1);
    }

    [Fact]
    public async Task SeedAsync_ExistingUser_DoesNotOverwrite()
    {
        // Arrange — manually create user with different attributes first
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var um = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var rm = setupScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await rm.CreateAsync(new IdentityRole("admin"));
            await um.CreateAsync(new ApplicationUser
            {
                UserName = "hans",
                RealName = "Pre-existing",
                Email = "old@example.com",
                IsActive = false,
                HomePath = "/legacy"
            }, "OldP@ss123");
        }

        // Act
        await IdentitySeeder.SeedAsync(_serviceProvider);

        // Assert — user remains untouched
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync("hans");
        Assert.NotNull(user);
        user!.RealName.Should().Be("Pre-existing");
        user.Email.Should().Be("old@example.com");
        user.IsActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class NullLoggerProvider : ILoggerProvider
    {
        public static readonly NullLoggerProvider Instance = new();
        public ILogger CreateLogger(string categoryName) => new NullLogger();
        public void Dispose() { }

        private sealed class NullLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
