# EF Core Code-First Rules

## Workflow

```
1. Define/Modify Entity class (Infrastructure/Identity/)
2. Update DbContext if new DbSet needed
3. Add Migration: dotnet ef migrations add <Name> --project backend/src/CGMSportFinance.Api
4. Review generated migration file
5. Apply: dotnet ef database update --project backend/src/CGMSportFinance.Api
```

## Migration Rules

- **Migration names**: PascalCase, descriptive (e.g., `AddAuditLogTable`, `AddMenuParentIndex`)
- **Never** manually edit migration `Up()`/`Down()` methods unless absolutely necessary
- **Never** delete a migration that has been applied to any environment
- **Always** review the generated migration before applying
- **Always** ensure `Down()` method correctly reverses the `Up()` operation

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

Connection strings are stored in `appsettings.secrets.enc.json` (AES-encrypted).

- **Never** hardcode connection strings
- **Never** commit plaintext connection strings
- Use `CGMSportFinance.SecretsCli` to manage encrypted values
- PostgreSQL (production) / SQLite (dev/test) — controlled by `Database:Provider`

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
