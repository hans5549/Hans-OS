# Hans-OS Copilot Instructions

## Quick Reference

| Item | Value |
|------|-------|
| **Tech Stack** | .NET 9.0, Vue 3, PostgreSQL, Ant Design Vue, Tailwind CSS |
| **Solution** | `backend/HansOS.slnx` |
| **Backend Build** | `dotnet build backend/HansOS.slnx` |
| **Backend Run** | `dotnet run --project backend/src/HansOS.Api` (Swagger: http://localhost:5180/swagger) |
| **Backend Test** | `dotnet test backend/HansOS.slnx` |
| **Frontend Dev** | `cd frontend && pnpm dev:antd` (port 5666) |
| **Frontend Type Check** | `cd frontend && pnpm check:type` |

> There is no standalone lint command; do not invent one.

---

## Architecture Overview

Monorepo containing a backend API + frontend SPA. Full architecture details in `.claude/ARCHITECTURE.md`.

### Backend (`backend/`)

ASP.NET Core Web API with three-layer architecture (Controller → Service → DbContext).

| Project | Type | Purpose |
|---------|------|---------|
| `backend/src/HansOS.Api` | Web API (net9.0) | Main application |
| `backend/tests/HansOS.Api.UnitTests` | xUnit | Service layer unit tests |
| `backend/tests/HansOS.Api.IntegrationTests` | xUnit | HTTP pipeline integration tests |

**Key Patterns**: Features-based folders | JWT + HttpOnly Refresh Token | EF Core Code-First | API Envelope (`ApiEnvelope<T>`)

### Frontend (`frontend/`)

Vue Vben Admin monorepo (pnpm + Turborepo).

- Main app: `frontend/apps/web-antd` (Vue 3 + Ant Design Vue + TypeScript strict mode)
- Shared packages: `frontend/packages/` (@core, stores, effects)

### Database

PostgreSQL, EF Core Code-First. Migrations are automatically applied on startup.

### Authentication

JWT bearer tokens + HttpOnly refresh token cookie.

### API Response Format

All successful responses use `ApiEnvelope<T>`:
```json
{ "code": 0, "data": { ... }, "error": null, "message": "ok" }
```
**Exception**: `POST /auth/refresh` returns a raw string (not an envelope).

---

## Testing Rules (Mandatory)

**Every new API endpoint and public method must have corresponding tests.**

| Change Type | Test Requirement |
|-------------|-----------------|
| New Controller endpoint | Must have corresponding integration test |
| New Service public method | Must have unit test or integration test |
| Modified endpoint behavior | Must update or add corresponding tests |
| Bug fix | Must have a test that reproduces the bug (red-green) |

### Test Naming Convention

- Format: `{Method}_{Scenario}_{ExpectedResult}`
- Example: `Login_WithValidCredentials_ReturnsAccessToken`
- Example: `GetMenuAll_Unauthorized_Returns401`

### Minimum Coverage Per Endpoint

1. Happy path (success scenario)
2. 401 Unauthorized (unauthenticated)
3. 400 Bad Request (invalid parameters, if applicable)
4. Business logic edge cases

---

## EF Core Migration Rules

- Migrations are automatically applied on deployment via `Program.cs` (`MigrateAsync()`)
- No seed data classes — initial data is handled via migration `Up()` methods
- Add migration: `dotnet ef migrations add <Name> --project backend/src/HansOS.Api`
- Apply locally: `dotnet ef database update --project backend/src/HansOS.Api`
- **Never** delete a migration that has been applied to any environment
- Migration names use PascalCase with descriptive naming (e.g., `AddAuditLogTable`)

---

## Git Conventions

### Commits

- **Format**: Conventional Commits 1.0.0
- **Description language**: Traditional Chinese
- Example: `feat(auth): 新增 JWT refresh token 自動續期機制`

### Branch Naming

- `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`

### Staging

- **Only stage specific files**, never use `git add .` or `git add -A`
- Use `--no-ff` when merging

---

## Coding Standards

### C# Conventions

- File-scoped namespaces (`namespace X;`)
- Private fields: `_camelCase`
- Async methods: `*Async` suffix
- Null checks: use `is null` / `is not null` (not `== null`)
- Empty strings: use `string.Empty` (not `""`)
- Prefer `record` for DTOs
- Methods should not exceed 20 lines
- Nesting should not exceed 3 levels

### Vue / TypeScript Conventions

- Use Composition API (`<script setup lang="ts">`), Options API is forbidden
- Pinia for state management, `use*Store` naming convention
- Ant Design Vue components + Tailwind CSS utility classes
- TypeScript strict mode

---

## Communication Style

- Reply in **Traditional Chinese** (`zh-TW`) unless the user explicitly requests another language
- Do not guess — use `[NEEDS CLARIFICATION]` when uncertain
- Do not bypass three-layer architecture

---

## Prerequisites (Mandatory)

**Before writing code**, read the corresponding document based on task type:

| Task Keywords | Required Reading |
|---------------|-----------------|
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.claude/ARCHITECTURE.md` |
| EF Core, Migration, DbContext, Entity | `.github/references/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.github/references/vue-frontend.md` |
| UI, Styling, Component Appearance | `.github/references/ui-style-guide.md` |
| Code Review, PR Review, 審查 | `.github/references/code-review-philosophy.md` |
| C#, Backend, Service, Controller | `.github/references/csharp-coding-style.md` |
| Communication, 溝通, 回覆風格 | `.github/references/communication-style.md` |

---

## Deployment

- Push to `main` → GitHub Actions auto-deploy
- Backend → Azure App Service (automatically applies pending migrations on startup)
- Frontend → Azure Static Web Apps
- Single environment (personal project, no staging/production split)
