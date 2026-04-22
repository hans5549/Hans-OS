# Architecture — Hans-OS

本文件是 `Codex` 版架構參考，內容對齊 `Hans-OS/.claude/ARCHITECTURE.md`，供 `Codex` 在此 repo 工作時閱讀。

## Monorepo Structure

```text
Hans-OS/
  backend/           ← .NET 9.0 Web API
  frontend/          ← Vue Vben Admin (pnpm monorepo)
  .claude/           ← Claude Code automation
  .Codex/            ← Codex-facing mirrored project rules
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
  Common/                  ← Cross-cutting concerns
  Options/                 ← Configuration option classes
  Controllers/             ← Presentation layer
  Services/                ← Business logic layer
  Models/                  ← DTOs
  Data/                    ← DbContext, entities, migrations
```

### Architecture Rules

1. **Three-layer architecture** — Controller → Service → DbContext
2. **API responses** — use `ApiEnvelope<T>`（`POST /auth/refresh` 為既有例外）
3. **EF Core Code-First** — migration 是合法且預期的流程
4. **Secrets via environment variables** — 不可硬編 connection string 或 signing key
5. **CORS with credentials** — 因 `HttpOnly` refresh cookie 為必要
6. **Frontend strict TypeScript** — 所有 `.ts` / `.vue` 必須通過 type check

## Database

### `ApplicationDbContext`

繼承 `IdentityDbContext<ApplicationUser>`，主要自訂資料表如下：

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

Migration 會由 `Program.cs` 的 `MigrateAsync()` 在啟動時自動套用。

## Authentication

### JWT + HttpOnly Refresh Token Flow

1. `POST /auth/login` → 驗證帳密，回傳 JWT + `jwt` cookie
2. `POST /auth/refresh` → 讀取 `jwt` cookie，rotate refresh token
3. `POST /auth/logout` → revoke refresh token 並清 cookie
4. `GET /auth/codes` → 回傳前端權限碼

### Cookie Settings

| Setting | Development | Production |
|---------|------------|------------|
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

- `frontend/apps/web-antd/src/api/request.ts` 使用 `RequestClient`
- `codeField = "code"`、`dataField = "data"`、`successCode = 0`
- 自動附加 `Authorization: Bearer <token>`
- `401` 自動 refresh

## Deployment

| Component | Target | Trigger |
|-----------|--------|---------|
| Backend | Azure App Service | Push to `main` |
| Frontend | Azure Static Web Apps | Push to `main` |

- 單一環境
- Health check：`GET /health`
