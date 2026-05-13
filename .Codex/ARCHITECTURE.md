# Architecture — Hans-OS

This is the `Codex` architecture reference, aligned with `Hans-OS/.claude/ARCHITECTURE.md`, for `Codex` to read when working in this repo.

## Monorepo Structure

```text
Hans-OS/
  backend/           <- .NET 9.0 Web API
  frontend/          <- Vue Vben Admin (pnpm monorepo)
  .claude/           <- Claude Code automation
  .Codex/            <- Codex-facing mirrored project rules
```

## Backend

### Projects

| Project | Type | Purpose |
|---------|------|---------|
| `backend/src/HansOS.Api` | Web API (net9.0) | Main application |
| `backend/tests/HansOS.Api.UnitTests` | xUnit | Service layer unit tests |
| `backend/tests/HansOS.Api.IntegrationTests` | xUnit | HTTP pipeline integration tests |

**Solution**: `backend/HansOS.slnx`

### Key Packages

- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Swashbuckle.AspNetCore`
- `Serilog.AspNetCore`

### Three-Layer Folder Structure

```text
HansOS.Api/
  Common/                  <- Cross-cutting concerns
  Options/                 <- Configuration option classes
  Controllers/             <- Presentation layer
  Services/                <- Business logic layer
  Models/                  <- DTOs
  Data/                    <- DbContext, entities, migrations
```

### Architecture Rules

1. **Three-layer architecture** — Controller -> Service -> DbContext
2. **API responses** — use `ApiEnvelope<T>` (`POST /auth/refresh` is an existing exception)
3. **EF Core Code-First** — migrations are the valid and expected workflow
4. **Secrets via environment variables** — do not hard-code connection strings or signing keys
5. **CORS with credentials** — required because the refresh cookie is `HttpOnly`
6. **Frontend strict TypeScript** — all `.ts` / `.vue` files must pass type check

## Database

### `ApplicationDbContext`

Inherits from `IdentityDbContext<ApplicationUser>`. Main custom tables:

| DbSet | Entity | Key |
|-------|--------|-----|
| `Menus` | `Menu` | `Guid Id` |
| `RefreshTokens` | `RefreshToken` | `Guid Id` |
| `RoleMenus` | `RoleMenu` | Composite `(RoleId, MenuId)` |

### Key Entities

- `ApplicationUser`
- `Menu`
- `RefreshToken`
- `RoleMenu`

### Migration Workflow

```bash
dotnet ef migrations add <Name> --project backend/src/HansOS.Api
dotnet ef database update --project backend/src/HansOS.Api
```

Migrations are automatically applied on startup by `Program.cs` via `MigrateAsync()`.

## Authentication

### JWT + HttpOnly Refresh Token Flow

1. `POST /auth/login` -> validate credentials and return JWT + `jwt` cookie
2. `POST /auth/refresh` -> read the `jwt` cookie and rotate the refresh token
3. `POST /auth/logout` -> revoke the refresh token and clear the cookie
4. `GET /auth/codes` -> return frontend permission codes

### Cookie Settings

| Setting | Development | Production |
|---------|-------------|------------|
| Name | `jwt` | `jwt` |
| HttpOnly | true | true |
| Secure | false | true |
| SameSite | Lax | None |

## Frontend

### Structure

```text
frontend/
  apps/
    web-antd/
      src/
        adapter/
        api/
        router/
        store/
        views/
  packages/
  internal/
```

### Key Patterns

- Vue 3 Composition API with `<script setup lang="ts">`
- Pinia
- Ant Design Vue
- Tailwind CSS
- TypeScript strict mode
- Frontend RBAC via menu / access code APIs

### Request Client

- `frontend/apps/web-antd/src/api/request.ts` uses `RequestClient`
- `codeField = "code"`, `dataField = "data"`, `successCode = 0`
- Automatically attaches `Authorization: Bearer <token>`
- Automatically refreshes on `401`

## Deployment

| Component | Target | Trigger |
|-----------|--------|---------|
| Backend | Azure App Service | Push to `main` |
| Frontend | Azure Static Web Apps | Push to `main` |

- Single environment
- Health check: `GET /health`
