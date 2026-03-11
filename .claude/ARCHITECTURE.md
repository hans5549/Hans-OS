# Architecture — Hans-OS (CGMSportFinance)

## Monorepo Structure

```
Hans-OS/
  backend/           ← .NET 9.0 Web API
  frontend/          ← Vue Vben Admin (pnpm monorepo)
  infra/             ← Docker / env templates
  .claude/           ← Claude Code automation
```

---

## Backend

### Projects

| Project | Type | Purpose |
|---------|------|---------|
| `backend/src/CGMSportFinance.Api` | Web API (net9.0) | Main application |
| `backend/src/CGMSportFinance.Secrets` | Class Library | Shared crypto utilities |
| `backend/tools/CGMSportFinance.SecretsCli` | Console App | CLI for managing encrypted secrets |
| `backend/tests/CGMSportFinance.Api.IntegrationTests` | xUnit | Integration tests |

**Solution**: `backend/CGMSportFinance.sln`

### Key NuGet Packages

- `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0
- `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0 (production)
- `Microsoft.EntityFrameworkCore.Sqlite` 9.0 (dev/test)
- `Swashbuckle.AspNetCore` 10.x (Swagger)

### Folder Structure (Features-Based)

```
CGMSportFinance.Api/
  Common/
    ApiEnvelope.cs            ← Success response wrapper
  Features/
    Auth/                     ← Login, Refresh, Logout, Permission Codes
      AuthController.cs
      ITokenService.cs
      JwtOptions.cs
      TokenIssueResult.cs
      Contracts/              ← Request/Response DTOs
    Menus/                    ← Dynamic menu tree
      MenuController.cs
      Contracts/
    Users/                    ← User info endpoint
      UserController.cs
      Contracts/
  Infrastructure/
    Configuration/            ← Encrypted config provider
    Identity/                 ← Entity classes (ApplicationUser, Menu, Permission, etc.)
    Persistence/              ← DbContext, Migrations
    Security/                 ← JWT token service implementation
```

### API Envelope Pattern

All successful responses use `ApiEnvelope<T>`:

```json
{ "code": 0, "data": { ... }, "error": null, "message": "ok" }
```

Error responses use ASP.NET Core `ProblemDetails`.

The frontend `RequestClient` unwraps `data` when `code === 0`.

---

## Database — EF Core Code-First + PostgreSQL

### DbContext: `ApplicationDbContext`

Inherits `IdentityDbContext<ApplicationUser, IdentityRole, string>` (ASP.NET Core Identity).

**Custom DbSets:**

| DbSet | Entity | Key |
|-------|--------|-----|
| `AuditLogs` | `AuditLog` | `long Id` |
| `Menus` | `Menu` | `Guid Id` |
| `Permissions` | `Permission` | `int Id` (unique `Code`) |
| `RefreshTokens` | `RefreshToken` | `Guid Id` |
| `RoleMenus` | `RoleMenu` | Composite `(RoleId, MenuId)` |
| `RolePermissions` | `RolePermission` | Composite `(RoleId, PermissionId)` |

### Key Entities

- **ApplicationUser**: extends `IdentityUser` with `Avatar`, `HomePath`, `RealName`, `IsActive`
- **Menu**: self-referencing tree (`ParentId → Parent/Children`), enum `MenuType` = Catalog/Menu/Button
- **RefreshToken**: stores SHA-256 hash (never plaintext), IP/UserAgent tracking, token chaining
- **Permission**: unique `Code` column (e.g., `"dashboard:analytics:view"`)
- **AuditLog**: write-only append log

### Migration Workflow

```bash
# Add migration
dotnet ef migrations add <Name> --project backend/src/CGMSportFinance.Api

# Apply to database
dotnet ef database update --project backend/src/CGMSportFinance.Api
```

---

## Authentication — JWT + HttpOnly Refresh Token

### Flow

1. `POST /api/auth/login` → validates credentials → returns JWT (body) + refresh token (`HttpOnly` cookie)
2. `POST /api/auth/refresh` → reads cookie → rotates tokens (revoke old, issue new pair)
3. `POST /api/auth/logout` → revokes refresh token → clears cookie
4. `GET /api/auth/codes` → returns `Permission.Code[]` for current user's roles (button-level RBAC)

### Configuration

| Section | Key Fields |
|---------|-----------|
| `Jwt` | `Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes` (15), `RefreshTokenDays` (14) |
| `Frontend` | `AllowedOrigins` (CORS with `AllowCredentials()`) |
| `Database` | `Provider` ("Postgres" / "Sqlite") |
All option classes use `.ValidateOnStart()`.

### Encrypted Secrets

`appsettings.secrets.enc.json` is AES-encrypted and committed safely. Custom `IConfigurationProvider` decrypts at startup. Managed via `CGMSportFinance.SecretsCli` tool.

---

## Frontend — Vue Vben Admin

### Structure (pnpm Monorepo + Turborepo)

```
frontend/
  apps/
    web-antd/               ← Main SPA (Vue 3 + Ant Design Vue)
      src/
        adapter/            ← Vben framework adapters
        api/
          core/             ← auth.ts, menu.ts, user.ts
          request.ts        ← Axios RequestClient (Bearer injection, 401 refresh)
        router/             ← Vue Router
        store/
          auth.ts           ← Pinia auth store (login, logout, fetchUserInfo)
        views/              ← Page components
  packages/                 ← Shared @core, stores, effects
  internal/                 ← Build tooling
```

### Key Patterns

- **Composition API** (`<script setup lang="ts">`) — Options API forbidden
- **Pinia** for state management
- **Ant Design Vue** for UI components
- **Tailwind CSS** for utility styling
- **TypeScript strict mode**
- **Frontend RBAC**: `getAccessCodesApi()` returns permission codes, used for button/menu visibility

### API Integration

- `src/api/request.ts`: configures `RequestClient` with `codeField: 'code'`, `dataField: 'data'`, `successCode: 0`
- Auto-injects `Authorization: Bearer <token>` header
- Auto-refreshes on 401 response
- Error display via `ant-design-vue` message component

---

## Roles

Three built-in roles are created by the `SeedEssentialData` EF Core migration:

| Role | Scope |
|------|-------|
| `super` | Full access |
| `admin` | Most operations |
| `user` | View-only |

No users, menus, or permissions are seeded. These are managed via the API/UI after deployment.

---

## Architecture Rules

1. **Features-based folders** — each feature has Controller + Contracts + optional Service
2. **No bypassing three-tier architecture** — Controller → Service → Repository/DbContext
3. **API responses** — always wrapped in `ApiEnvelope<T>` for success
4. **EF Core Code-First** — migrations allowed and expected
5. **Encrypted secrets** — never store plaintext connection strings or signing keys
6. **CORS with credentials** — required for `HttpOnly` refresh token cookie
7. **Frontend strict TypeScript** — all `.ts`/`.vue` files must pass `pnpm check:type`
