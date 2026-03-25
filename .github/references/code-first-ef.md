---
description: 'EF Core Code-First rules'
---

# EF Core Code-First Rules

## Migration Workflow

```
1. Define/modify the Entity class
2. If a new DbSet is needed, update the DbContext
3. Add Migration: dotnet ef migrations add <Name> --project backend/src/HansOS.Api
4. Review the generated migration file
5. Apply: dotnet ef database update --project backend/src/HansOS.Api
```

## Migration Rules

- **Naming**: PascalCase, descriptive (e.g., `AddAuditLogTable`, `AddMenuParentIndex`)
- **Never** manually edit a migration's `Up()`/`Down()` methods unless absolutely necessary
- **Never** delete a migration that has been applied to any environment
- **Must** review the generated migration before applying
- **Must** ensure the `Down()` method correctly reverses the `Up()` operation
- Migrations are automatically applied on startup via `Program.cs` (`MigrateAsync()`)

## Entity Design

- Use `Guid` as the primary key for new entities
- Minimize data annotations — prefer Fluent API in `OnModelCreating`
- Navigation properties: always configure in `OnModelCreating`
- Composite keys: use `HasKey(e => new { e.KeyA, e.KeyB })`

## Query Rules

- **AsNoTracking()** — for all read-only queries
- **Include()** — eager loading to avoid N+1
- **Select() projection** — retrieve only the required fields
- **Where()** — filter at the database level, not in memory
- **FirstOrDefaultAsync** — preferred over **SingleOrDefaultAsync** unless uniqueness must be enforced

## Connection Strings

- **Never** hardcode connection strings
- **Never** commit plaintext connection strings
- Local development: use `appsettings.Development.json` (gitignored)
- Production: use Azure App Service environment variables

## Common Patterns

```csharp
// Read-only query + projection
var result = await context.Menus
    .AsNoTracking()
    .Where(m => m.ParentId == null)
    .Select(m => new MenuDto(m.Id, m.Name, m.Path))
    .ToListAsync(cancellationToken);

// Including navigation
var user = await context.Users
    .Include(u => u.RefreshTokens.Where(t => t.IsActive))
    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
```

## Existing Entity Reference

| Entity | Primary Key | Description |
|--------|-------------|-------------|
| `ApplicationUser` | `string Id` (Identity) | Extends `IdentityUser`, includes Avatar, HomePath, RealName, IsActive |
| `Menu` | `Guid Id` | Self-referencing tree structure (ParentId → Parent/Children), enum MenuType |
| `RefreshToken` | `Guid Id` | Stores SHA-256 hash (not plaintext) |
| `RoleMenu` | Composite `(RoleId, MenuId)` | Role-to-menu assignment |
