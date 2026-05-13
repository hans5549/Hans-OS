# C# Backend Coding Style

Use this for backend changes under `backend/src/HansOS.Api` and backend tests.

## Naming

| Type | Convention | Example |
|------|------------|---------|
| Class / struct / enum | PascalCase | `BankTransactionService` |
| Method | PascalCase | `GetSummaryAsync` |
| Interface | `I` + PascalCase | `IAuthService` |
| Public property | PascalCase | `AccessToken` |
| Private field | `_camelCase` | `_logger` |
| Parameter / local variable | camelCase | `userId` |
| Constant | PascalCase | `DefaultPageSize` |
| Async method | `Async` suffix | `LoginAsync` |

## File Structure

- Use file-scoped namespaces.
- One primary type per file.
- `using` order: System, Microsoft, third-party, project-internal.
- Keep DTOs under `Models/<Domain>`.
- Keep service interfaces and implementations under `Services/`.
- Keep EF configuration under `Data/Configurations/<Domain>/`.

## Architecture

- Preserve Controller -> Service -> DbContext.
- Controllers should stay thin: auth, route, model binding, envelope return.
- Services own business rules and persistence coordination.
- Do not call DbContext from Vue or controllers when an existing service boundary applies.
- Use DI constructor injection.
- Services should normally be scoped.

## API and Validation

- Standard endpoints return `ApiEnvelope<T>`.
- `POST /auth/refresh` is the known raw-string exception.
- Request models should use validation attributes on positional `record` parameters when appropriate.
- User identity should come from claims, not request body.
- Controllers that require login should use `[Authorize]`.
- Preserve current route naming style: no `/api` prefix in controller routes unless the existing route uses it.

## Nulls, Strings, and DTOs

- Prefer `is null` / `is not null`.
- Prefer `string.Empty` for intentional empty strings.
- Prefer `record` for DTOs.
- Prefer `init` for immutable properties.
- Avoid `default!` unless the framework requires it and the invariant is obvious.

## Async

- Keep async end-to-end.
- Do not use sync-over-async.
- Accept and pass `CancellationToken`.
- Use `Task` by default; use `ValueTask` only with measured need.

## EF Core

- Use `AsNoTracking()` for read-only queries.
- Use projection with `Select()` for API response shapes.
- Use `Include()` only when the entity graph is needed.
- Keep filters in SQL, not in memory after materialization.
- Avoid N+1 and unbounded list loads.
- Define money precision in Fluent API.

## Logging and Errors

Use structured logging placeholders:

```csharp
logger.LogInformation("User {UserId} logged in", userId);
```

- Do not use string interpolation in log templates.
- Use precise exception types.
- Let `GlobalExceptionHandler` translate errors.
- Do not swallow exceptions without a clear reason and logging.

## Method Quality

- Prefer guard clauses to deep nesting.
- Keep nesting under 3 levels where practical.
- Keep methods short and single-purpose.
- Do not introduce single-use abstractions unless they remove real complexity.

## Comments

- Comments explain why, not what.
- Keep comments Traditional Chinese for Hans-OS-specific code.
- Do not edit generated migration designer code except through EF tooling.
