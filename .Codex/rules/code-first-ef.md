# EF Core Code-First Rules

Hans-OS 使用 EF Core Code-First + PostgreSQL。`ApplicationDbContext` 繼承 `IdentityDbContext<ApplicationUser>`，並以 `ApplyConfigurationsFromAssembly(...)` 載入 Fluent API configuration。

## Workflow

```bash
# 1. Define or modify the entity
# 2. Update DbContext / Fluent API configuration if needed
dotnet ef migrations add <Name> --project backend/src/HansOS.Api

# 3. Inspect generated migration and model snapshot
dotnet ef database update --project backend/src/HansOS.Api
```

## Migration Rules

- Migration names must use PascalCase and be descriptive.
- Do not manually edit `Up()` / `Down()` unless necessary.
- Never delete a migration that has been applied in any environment.
- Always review generated migration and `ApplicationDbContextModelSnapshot`.
- `Down()` must roll back correctly.
- Migrations run during startup through `DatabaseStartupService`.
- Keep seed data idempotent. Re-running startup tasks must not duplicate data.

## DbContext Rules

- Add new aggregate tables as `DbSet<T>` only when they are queried directly or useful for clarity.
- Keep relationship, index, max length, delete behavior, precision, and seed constraints in `Data/Configurations/*`.
- Prefer existing configuration folders:
  - `Activities`
  - `Finance`
  - `Identity`
  - `Menu`
  - `Organization`
  - `PersonalFinance`
- If a new domain folder is truly needed, name it after the domain, not after a technical layer.

## Entity Design

- Prefer `Guid` primary keys for new domain entities unless an existing contract dictates otherwise.
- Minimize data annotations on entities; prefer Fluent API for persistence rules.
- Use `record` DTOs for request/response models, not EF entities.
- Keep navigation properties intentional. Do not add bidirectional navigation just because EF supports it.
- Use `HasKey(...)` for composite keys.
- Define decimal precision explicitly for money-like values.

## Query Rules

- Use `AsNoTracking()` for read-only queries.
- Avoid N+1. Use explicit `Include()` only when the entity graph is needed.
- Prefer `Select()` projection for API response shapes.
- Push filters down to the database. Do not materialize first and filter in memory.
- Prefer pagination for list endpoints that can grow.
- Prefer `FirstOrDefaultAsync` over `SingleOrDefaultAsync` unless uniqueness is enforced and violation should fail.
- Pass `CancellationToken` through EF async calls.

## Startup and Tests

- `DatabaseStartupService` applies migrations and runs `IdentitySeeder`.
- Integration tests replace EF/Npgsql registrations with EF InMemory in `HansOsWebApplicationFactory`.
- Do not write code that only works with InMemory provider if production PostgreSQL semantics differ.
- For bug fixes involving EF behavior, prefer tests that cover the real query shape and edge case.

## Secrets

- Do not hard-code connection strings.
- Do not commit plaintext secrets.
- Use `appsettings.Development.json` for local development if needed; it should remain gitignored.
- Use environment variables in production, for example `ConnectionStrings__DefaultConnection`.
