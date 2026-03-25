---
applyTo:
  - 'backend/**/*.cs'
  - 'backend/**/*.razor'
  - 'backend/**/*.csproj'
---

# C# Backend Coding Rules

## Naming Conventions

- File-scoped namespaces (`namespace X;`)
- Private fields: `_camelCase`
- Async methods: `*Async` suffix
- Constants: `PascalCase`
- Interfaces: `I` prefix (`IAuthService`)

## Code Style

- Null checks: `is null` / `is not null` (not `== null`)
- Empty strings: `string.Empty` (not `""`)
- Prefer `record` for DTOs (immutable, value equality)
- Prefer primary constructors for DI
- Methods should not exceed 20 lines
- Nesting should not exceed 3 levels
- Always use braces for `if`/`else`/`for`/`while`

## Architecture

- **Three-layer**: Controller → Service → DbContext (no bypassing)
- **API responses**: always wrapped in `ApiEnvelope<T>` (except refresh endpoint)
- **Dependency Injection**: constructor injection, scoped lifetime for services

## EF Core Code-First

- **AsNoTracking()** for all read-only queries
- **Include()** for eager loading — avoid N+1
- **Select() projection** to fetch only needed columns
- **Where()** at database level — never filter in memory after materialization
- Migrations auto-applied on startup via `MigrateAsync()`
- Migration names: PascalCase, descriptive (e.g., `AddAuditLogTable`)
- **Never** delete a migration that has been applied to any environment
- Use Guid for primary keys (new entities)
- Prefer Fluent API over Data Annotations for relationships

## Error Handling

- Use `GlobalExceptionHandler` pattern
- `ArgumentException` messages pass through to clients
- Generic Chinese error messages for unhandled exceptions
- Stack traces only logged server-side

## Authentication

- JWT bearer tokens + HttpOnly refresh token cookie
- User ID from `ClaimTypes.NameIdentifier` (never from request body)
- `[Authorize]` at class level on controllers
- Email updates via `SetEmailAsync()` (not direct property assignment)

## Validation

- Data Annotations on positional record parameters
- Pattern: `public record XxxRequest([Required][StringLength(N)] string Field, ...)`
- Model validation runs automatically via ASP.NET Core pipeline
