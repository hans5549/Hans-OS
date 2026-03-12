# EF Core Code-First Rules

## Workflow

```
1. Define/Modify Entity class (Infrastructure/Identity/)
2. Update DbContext if new DbSet needed
3. Add Migration: dotnet ef migrations add <Name> --project backend/src/HansOS.Api
4. Review generated migration file
5. Apply: dotnet ef database update --project backend/src/HansOS.Api
```

## Migration Rules

- **Migration names**: PascalCase, descriptive (e.g., `AddAuditLogTable`, `AddMenuParentIndex`)
- **Never** manually edit migration `Up()`/`Down()` methods unless absolutely necessary
- **Never** delete a migration that has been applied to any environment
- **Always** review the generated migration before applying
- **Always** ensure `Down()` method correctly reverses the `Up()` operation
- Migrations are **auto-applied on startup** via `Program.cs` (`MigrateAsync()`)

## Entity Design

- Use `Guid` for primary keys (new entities)
- Use data annotations sparingly — prefer Fluent API in `OnModelCreating`
- Navigation properties: always configure in `OnModelCreating`
- Composite keys: configure via `HasKey(e => new { e.KeyA, e.KeyB })`

## Query Rules

- **AsNoTracking()** for all read-only queries
- **Include()** for eager loading — avoid N+1
- **Select() projection** to fetch only needed columns
- **Where()** at database level — never filter in memory after materialization
- **FirstOrDefaultAsync** over **SingleOrDefaultAsync** unless uniqueness must be enforced

## Connection String

Connection strings are configured via environment variables in production (Azure App Service).

- **Never** hardcode connection strings
- **Never** commit plaintext connection strings
- Local dev: use `appsettings.Development.json` (gitignored)
- Production: set `ConnectionStrings__DefaultConnection` as App Service env var

## Common Patterns

```csharp
// Read-only query with projection
var result = await context.Menus
    .AsNoTracking()
    .Where(m => m.ParentId == null)
    .Select(m => new MenuDto(m.Id, m.Name, m.Path))
    .ToListAsync(cancellationToken);

// Include navigation
var user = await context.Users
    .Include(u => u.RefreshTokens.Where(t => t.IsActive))
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
```
