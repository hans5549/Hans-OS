using System.Text;
using HansOS.Api.Common;
using HansOS.Api.Common.Observability;
using HansOS.Api.Data;
using HansOS.Api.Data.Entities;
using HansOS.Api.Data.Seeding;
using HansOS.Api.Options;
using HansOS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Options ──────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
builder.Services.Configure<IdentitySeedOptions>(builder.Configuration.GetSection(IdentitySeedOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var frontendOptions = builder.Configuration.GetSection(FrontendOptions.SectionName).Get<FrontendOptions>() ?? new FrontendOptions();
var identitySeedOptions = builder.Configuration.GetSection(IdentitySeedOptions.SectionName).Get<IdentitySeedOptions>()
    ?? new IdentitySeedOptions();
ValidateRuntimeOptions(builder.Environment, jwtOptions, frontendOptions, identitySeedOptions);

// ── EF Core + Identity ───────────────────────────
builder.Services.AddSingleton<SlowQueryInterceptor>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, opt) =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    opt.AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>());
});

builder.Services.AddIdentityCore<ApplicationUser>(opt =>
    {
        opt.Password.RequireDigit = true;
        opt.Password.RequiredLength = 8;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireLowercase = true;
        opt.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// ── JWT Authentication ───────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendOptions.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Services DI ──────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ITsfSettingsService, TsfSettingsService>();
builder.Services.AddScoped<IBankTransactionService, BankTransactionService>();
builder.Services.AddScoped<IBankTransactionImportService, BankTransactionImportService>();
builder.Services.AddScoped<IBankTransactionExcelExportService, BankTransactionExcelExportService>();
builder.Services.AddScoped<IBankTransactionReceiptService, BankTransactionReceiptService>();
builder.Services.AddScoped<IAnnualBudgetService, AnnualBudgetService>();
builder.Services.AddScoped<IBudgetImportService, BudgetImportService>();
builder.Services.AddScoped<IBudgetShareService, BudgetShareService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IPendingRemittanceService, PendingRemittanceService>();
builder.Services.AddScoped<IFinanceTaskService, FinanceTaskService>();
builder.Services.AddScoped<IUnifiedTaskService, UnifiedTaskService>();
builder.Services.AddScoped<IFinanceAccountService, FinanceAccountService>();
builder.Services.AddScoped<ITransactionCategoryService, TransactionCategoryService>();
builder.Services.AddScoped<IFinanceTransactionService, FinanceTransactionService>();
builder.Services.AddScoped<IFinanceTransactionAnalyticsService, FinanceTransactionAnalyticsService>();
builder.Services.AddScoped<IStockTransactionService, StockTransactionService>();
builder.Services.AddScoped<IArticleBookmarkService, ArticleBookmarkService>();
builder.Services.AddScoped<IArticleBookmarkGroupService, ArticleBookmarkGroupService>();
builder.Services.AddScoped<ITodoProjectService, TodoProjectService>();
builder.Services.AddScoped<ITodoItemService, TodoItemService>();
builder.Services.AddScoped<ITodoCategoryService, TodoCategoryService>();
builder.Services.AddScoped<ITodoTagService, TodoTagService>();

// ── Controllers + Swagger ────────────────────────
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(opt =>
    {
        opt.InvalidModelStateResponseFactory = context =>
        {
            var error = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "請求資料驗證失敗" : e.ErrorMessage)
                .FirstOrDefault() ?? "請求資料驗證失敗";

            return new BadRequestObjectResult(ApiEnvelope<object>.Fail(error, "驗證失敗"));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Hans-OS API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "輸入 JWT token"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Health Checks ────────────────────────────────
// liveness (/healthz)  = process alive; no dependencies
// readiness (/readyz)  = DbContext reachable
// legacy   (/health)   = all checks (backward compat)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);

// ── Exception Handler ────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Migrate + Seed ───────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
}

await IdentitySeeder.SeedAsync(app.Services);

// ── Middleware Pipeline ──────────────────────────
app.UseExceptionHandler(_ => { });
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});
app.MapHealthChecks("/readyz", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
app.MapHealthChecks("/health"); // 保留給既有監控腳本，執行所有檢查

app.Run();

static void ValidateRuntimeOptions(
    IHostEnvironment environment,
    JwtOptions jwtOptions,
    FrontendOptions frontendOptions,
    IdentitySeedOptions identitySeedOptions)
{
    if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
        || string.IsNullOrWhiteSpace(jwtOptions.Audience)
        || string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
        || jwtOptions.SigningKey.Length < 32)
    {
        throw new InvalidOperationException("Jwt 設定不完整或 SigningKey 長度不足");
    }

    if (!environment.IsDevelopment()
        && jwtOptions.SigningKey == JwtOptions.DevelopmentSigningKey)
    {
        throw new InvalidOperationException("非 Development 環境不可使用預設 Jwt:SigningKey");
    }

    if (frontendOptions.AllowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Frontend:AllowedOrigins 至少需要設定一個來源");
    }

    if (string.IsNullOrWhiteSpace(identitySeedOptions.AdminRoleName)
        || string.IsNullOrWhiteSpace(identitySeedOptions.AdminUserName)
        || string.IsNullOrWhiteSpace(identitySeedOptions.AdminRealName)
        || string.IsNullOrWhiteSpace(identitySeedOptions.AdminEmail)
        || string.IsNullOrWhiteSpace(identitySeedOptions.AdminHomePath))
    {
        throw new InvalidOperationException("IdentitySeed 設定不完整");
    }

    if (!environment.IsDevelopment()
        && string.IsNullOrWhiteSpace(identitySeedOptions.AdminPassword))
    {
        throw new InvalidOperationException("非 Development 環境必須設定 IdentitySeed:AdminPassword");
    }
}

public partial class Program;
