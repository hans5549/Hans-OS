using System.Reflection;
using System.Text;
using CGMSportFinance.Api.Features.Auth;
using CGMSportFinance.Api.Infrastructure.Configuration;
using CGMSportFinance.Api.Infrastructure.Identity;
using CGMSportFinance.Api.Infrastructure.Persistence;
using CGMSportFinance.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEncryptedJsonFileBeforeEnvironmentVariables("appsettings.secrets.enc.json", optional: true);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services
    .AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .Validate(options => options.AllowedOrigins.Count > 0, "At least one frontend origin must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), "Database:Provider is required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "Jwt:SigningKey is required.")
    .Validate(options => options.AccessTokenMinutes > 0, "Jwt:AccessTokenMinutes must be greater than 0.")
    .Validate(options => options.RefreshTokenDays > 0, "Jwt:RefreshTokenDays must be greater than 0.")
    .ValidateOnStart();

var connectionString = ResolveConnectionString(builder.Configuration);
var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                      ?? new DatabaseOptions();

if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("A database connection string or PG* environment variables must be configured in Production.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseOptions.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddHttpContextAccessor();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Jwt configuration is missing.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = signingKey,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = jwtOptions.Audience,
            ValidIssuer = jwtOptions.Issuer,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var frontendOptions = builder.Configuration.GetSection(FrontendOptions.SectionName).Get<FrontendOptions>()
                              ?? new FrontendOptions();

        policy
            .WithOrigins(frontendOptions.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "Paste the access token as: Bearer {token}",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Scheme = "bearer",
        Type = SecuritySchemeType.Http,
    };

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "Core admin APIs for CGMSportFinance. Refresh token rotation uses an HttpOnly cookie named `refresh_token`.",
        Title = "CGMSportFinance API",
        Version = "v1",
    });

    options.AddSecurityDefinition("Bearer", bearerScheme);

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", null, null)] = []
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

await InitializeDatabaseAsync(app.Services);

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var isSqlite = db.Database.ProviderName is not null
        && db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

    if (isSqlite)
    {
        await db.Database.EnsureCreatedAsync();
        // EnsureCreatedAsync does not run migrations, so seed roles manually for dev/test.
        await SeedRolesIfMissingAsync(scope.ServiceProvider);
    }
    else
    {
        await db.Database.MigrateAsync();
    }
}

static async Task SeedRolesIfMissingAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in (string[])["super", "admin", "user"])
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}

app.Run();

static string ResolveConnectionString(ConfigurationManager configuration)
{
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT");
    var pgSslMode = Environment.GetEnvironmentVariable("PGSSLMODE");

    if (!string.IsNullOrWhiteSpace(pgHost) &&
        !string.IsNullOrWhiteSpace(pgDatabase) &&
        !string.IsNullOrWhiteSpace(pgUser) &&
        !string.IsNullOrWhiteSpace(pgPassword))
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Database = pgDatabase,
            Host = pgHost,
            Password = pgPassword,
            Port = int.TryParse(pgPort, out var port) ? port : 5432,
            Username = pgUser,
        };

        if (!string.IsNullOrWhiteSpace(pgSslMode) &&
            Enum.TryParse<SslMode>(pgSslMode, ignoreCase: true, out var sslMode))
        {
            builder.SslMode = sslMode;
        }
        else if (pgHost.Contains(".postgres.database.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }

    var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configuredConnectionString))
    {
        return configuredConnectionString;
    }

    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
}

public partial class Program;
