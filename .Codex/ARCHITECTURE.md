# Architecture - Hans-OS

Codex 使用這份文件理解目前 Hans-OS 的實際專案形狀。此檔依目前 repo 重新整理，不是舊版 auth/menu-only 摘要。

## Monorepo Structure

```text
Hans-OS/
  backend/        .NET 9 Web API, EF Core, PostgreSQL, Identity, xUnit tests
  frontend/       Vue Vben Admin pnpm monorepo, main app at apps/web-antd
  docs/           Deployment notes
  .Codex/         Codex settings, rules, agents, skills
  .claude/        Claude Code automation and hooks
  .github/        GitHub Copilot CLI instructions, hooks, workflows
```

## Backend

### Projects

| Project | Type | Purpose |
|---------|------|---------|
| `backend/src/HansOS.Api` | ASP.NET Core Web API, `net9.0` | Main API application |
| `backend/tests/HansOS.Api.UnitTests` | xUnit | Service, model, seeding, and DbContext tests |
| `backend/tests/HansOS.Api.IntegrationTests` | xUnit + `WebApplicationFactory<Program>` | HTTP pipeline and endpoint tests |

Solution: `backend/HansOS.slnx`

### Key Packages

- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `Serilog.AspNetCore`
- `Swashbuckle.AspNetCore`
- `ClosedXML`
- Test packages: `xunit`, `FluentAssertions`, `NSubstitute`, `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.AspNetCore.Mvc.Testing`

### Runtime Pipeline

`Program.cs` configures:

- Serilog request logging
- strongly typed options: `Jwt`, `Frontend`, `IdentitySeed`
- runtime option validation, including production JWT signing key checks
- EF Core PostgreSQL plus `SlowQueryInterceptor`
- ASP.NET Identity roles and users
- JWT bearer authentication
- CORS with credentials
- controller model validation returning `ApiEnvelope<object>.Fail(...)`
- Swagger in development
- health checks: `/healthz` liveness, `/readyz` database readiness, `/health` legacy all checks
- `DatabaseStartupService` hosted service for migrations and identity seed

### Layering

```text
Controllers/      HTTP surface, auth attributes, ApiEnvelope return shape
Services/         Business logic, validation, transactions, export/import logic
Models/           Request/response DTOs
Data/             DbContext, entities, Fluent API configurations, migrations, seed data
Common/           ApiEnvelope, exceptions, global exception handler, observability
Options/          JwtOptions, FrontendOptions, IdentitySeedOptions
```

Required direction: Controller -> Service -> DbContext. Do not put business logic directly in controllers or Vue components.

### Backend Modules

| Area | Controllers / routes | Main services | Notes |
|------|----------------------|---------------|-------|
| Auth / user / menu | `auth`, `user`, `menu` | `AuthService`, `UserService`, `MenuService` | JWT access token, HttpOnly refresh cookie, backend-driven menu tree |
| API spec | `api-spec` | controller-only | Runtime endpoint inventory |
| TSF settings | `tsf-settings` | `TsfSettingsService` | Departments and bank initial balances |
| Bank transactions | `bank-transactions` | `BankTransactionService`, import/export/receipt services | Bank records, department assignment, receipt tracking, Excel import/export |
| Annual budget | `annual-budgets`, `public/department-budget` | `AnnualBudgetService`, `BudgetImportService`, `BudgetShareService` | Department budgets, share tokens, public budget edit flow |
| Activities | `activities` | `ActivityService` | Activities, groups, expenses, month summaries |
| Pending remittances | `pending-remittances` | `PendingRemittanceService` | CRUD plus completion flow |
| Finance tasks | `finance-tasks` | `FinanceTaskService`, `UnifiedTaskService` | Finance task CRUD and unified task view |
| Personal finance | `finance/accounts`, `finance/categories`, `finance/transactions`, `finance/stocks` | account/category/transaction/analytics/stock services | Personal bookkeeping, analytics, stock holdings |

### API Contract

- Standard success response: `ApiEnvelope<T>.Success(data)`
- Standard error response: `ApiEnvelope<T>.Fail(error, message)`
- Model validation errors are normalized through `InvalidModelStateResponseFactory`.
- `GlobalExceptionHandler` maps exceptions into API envelopes.
- Existing exception: `POST /auth/refresh` returns a raw access token string because the frontend calls it through `baseRequestClient`.

### Authentication

Flow:

1. `POST /auth/login` validates credentials, returns `LoginResponse { accessToken }`, and sets `jwt` HttpOnly refresh cookie.
2. `POST /auth/refresh` reads the `jwt` cookie, revokes the old refresh token, saves a new hashed refresh token, sets a new cookie, and returns a new access token string.
3. `POST /auth/logout` revokes the refresh token if present and clears the cookie.
4. `GET /auth/codes` returns access codes for frontend permission checks.

Cookie behavior:

| Setting | Development | Production |
|---------|-------------|------------|
| Name | `jwt` | `jwt` |
| HttpOnly | true | true |
| Secure | false | true |
| SameSite | Lax | None |
| Path | `/` | `/` |

Security-sensitive values:

- `ConnectionStrings__DefaultConnection`
- `Jwt__SigningKey`
- `IdentitySeed__AdminPassword`
- `Frontend__AllowedOrigins__0`

Do not hard-code real secrets in source.

## Database

`ApplicationDbContext` inherits from `IdentityDbContext<ApplicationUser>` and uses `builder.ApplyConfigurationsFromAssembly(...)`.

### DbSets

| DbSet | Entity |
|-------|--------|
| `Menus` | `Menu` |
| `RefreshTokens` | `RefreshToken` |
| `RoleMenus` | `RoleMenu` |
| `SportsDepartments` | `SportsDepartment` |
| `BankInitialBalances` | `BankInitialBalance` |
| `BankTransactions` | `BankTransaction` |
| `AnnualBudgets` | `AnnualBudget` |
| `DepartmentBudgets` | `DepartmentBudget` |
| `BudgetItems` | `BudgetItem` |
| `Activities` | `Activity` |
| `ActivityGroups` | `ActivityGroup` |
| `ActivityExpenses` | `ActivityExpense` |
| `PendingRemittances` | `PendingRemittance` |
| `FinanceAccounts` | `FinanceAccount` |
| `TransactionCategories` | `TransactionCategory` |
| `FinanceTransactions` | `FinanceTransaction` |
| `StockTransactions` | `StockTransaction` |
| `BudgetShareTokens` | `BudgetShareToken` |
| `FinanceTasks` | `FinanceTask` |

### Configuration Folders

```text
Data/Configurations/
  Activities/
  Finance/
  Identity/
  Menu/
  Organization/
  PersonalFinance/
```

Prefer Fluent API in these configuration classes over scattered annotations.

### Migration Workflow

```bash
dotnet ef migrations add <Name> --project backend/src/HansOS.Api
dotnet ef database update --project backend/src/HansOS.Api
```

Migrations are auto-applied by `DatabaseStartupService`. Do not delete migrations that may have been applied anywhere.

## Frontend

### Project Shape

```text
frontend/
  package.json              Vben monorepo scripts
  pnpm-lock.yaml
  apps/web-antd/
    src/
      api/core/             Auth, menu, TSF, finance, budget APIs
      api/request.ts        RequestClient config
      router/               Guards, access generation, local static routes
      store/auth.ts         Login/logout/fetchUserInfo
      views/                Page components
  packages/                 Shared Vben packages
  internal/                 Build tooling
```

Important scripts:

```bash
cd frontend && pnpm dev:antd
cd frontend && pnpm check:type
cd frontend && pnpm build:antd
```

### Request Client

`frontend/apps/web-antd/src/api/request.ts` configures:

- `codeField: "code"`
- `dataField: "data"`
- `successCode: 0`
- bearer token injection from access store
- `Accept-Language` from preferences
- automatic refresh on 401 through `refreshTokenApi()`
- Ant Design message-based error display

Use API wrapper files under `frontend/apps/web-antd/src/api/core/`; do not call backend endpoints inline in components.

### Routing and Access

- Local route modules currently live under `frontend/apps/web-antd/src/router/routes/modules/`.
- Backend-driven menus are loaded by `getAllMenusApi()` inside `router/access.ts`.
- `generateAccessible()` maps backend menu records to Vue route components through `import.meta.glob('../views/**/*.vue')`.
- Public or core routes use `meta.ignoreAccess` / auth paths; authenticated routes depend on access token and generated access routes.

### Main Frontend Views

Current business views include:

- `taiwan-sports-finance`: overview, bank pages, settings, tasks, pending remittance, annual budget, activities, receipt tracking
- `finance`: personal finance settings, stocks, transactions, reports
- `public/budget`: public budget share page
- `zhongyuan-mission`
- `system-design`
- Vben core views and demos

### UI Rules

- Vue 3 Composition API with `<script setup lang="ts">`.
- Pinia for state.
- Ant Design Vue for base components.
- Tailwind utility classes and Vben design tokens.
- CSS variables and HSL color tokens for new theme colors.
- Support light and dark mode.

## Tests

- Unit tests: `backend/tests/HansOS.Api.UnitTests`
- Integration tests: `backend/tests/HansOS.Api.IntegrationTests`
- Integration tests use `HansOsWebApplicationFactory`, set environment to Development, override JWT config, and replace EF/Npgsql with EF InMemory per test database.
- Test naming convention: `{Method}_{Scenario}_{ExpectedResult}`.
- Bug fixes require a failing/reproducing test before the implementation.

## Deployment

Backend:

- Workflow: `.github/workflows/deploy-backend.yml`
- Trigger: push to `main` with `backend/**` or workflow changes, plus manual dispatch
- Build/test/publish Release
- Deploy target: Azure App Service `hans-os-api`
- Runtime settings include JWT signing key, DB connection string, identity seed password, frontend allowed origin, `ASPNETCORE_ENVIRONMENT=Production`

Frontend:

- Workflow: `.github/workflows/frontend-static-web-apps.yml`
- Trigger: push to `main` with `frontend/**` or workflow changes, plus manual dispatch
- Node `24.13.0`, pnpm `10.30.3`
- Runs `pnpm install --frozen-lockfile` and `pnpm build:antd`
- Deploys prebuilt `frontend/apps/web-antd/dist` to Azure Static Web Apps

See also: `docs/deployment.md`.

## Architecture Rules

1. Keep Controller -> Service -> DbContext layering.
2. Use `ApiEnvelope<T>` for standard endpoints.
3. Preserve JWT + HttpOnly refresh token semantics.
4. Preserve backend-driven menu / route / permission behavior.
5. Use EF Core Code-First and Fluent API configuration.
6. Do not break the migration chain.
7. Do not hard-code secrets.
8. Keep frontend strict TypeScript green.
9. Preserve existing Vben request, route, store, and design token patterns.
