# EF Core Code-First Rules

## Workflow

```bash
1. Define / modify the Entity
2. If needed, update DbContext / Fluent API
3. Add a Migration:
   dotnet ef migrations add <Name> --project backend/src/HansOS.Api
4. Inspect the generated migration
5. Apply it:
   dotnet ef database update --project backend/src/HansOS.Api
```

## Migration Rules

- Migration names must use **PascalCase** and be descriptive.
- Do not manually edit `Up()` / `Down()` unless necessary.
- **Never delete** a migration that has been applied in any environment.
- Always review generated migrations.
- `Down()` must roll back correctly.
- Migrations are automatically applied on startup by `Program.cs` via `MigrateAsync()`.

## Entity Design

- Prefer `Guid` primary keys for new entities.
- Minimize data annotations; prefer Fluent API.
- Keep navigation properties and relationship configuration centralized in `OnModelCreating` when possible.
- Use `HasKey(...)` for composite keys.

## Query Rules

- Prefer `AsNoTracking()` for read-only queries.
- Avoid N+1. Use explicit `Include()` when needed.
- Prefer `Select()` projection and fetch only required fields.
- Push filters down to the database as much as possible. Do not materialize first and then filter in memory.
- Prefer `FirstOrDefaultAsync` over `SingleOrDefaultAsync` unless uniqueness must be enforced.

## Secrets

- Do not hard-code connection strings.
- Do not commit plaintext secrets.
- Use `appsettings.Development.json` for local development (gitignored).
- Use environment variables in production, for example `ConnectionStrings__DefaultConnection`.
