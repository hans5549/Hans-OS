# Hans-OS — Copilot CLI Instructions

## Quick Facts

| Item | Value |
|------|-------|
| **Stack** | .NET 9.0, Vue 3, PostgreSQL, Ant Design Vue, Tailwind CSS |
| **Solution** | `backend/HansOS.slnx` |
| **Backend Build** | `dotnet build backend/HansOS.slnx` |
| **Backend Run** | `dotnet run --project backend/src/HansOS.Api` |
| **Backend Test** | `dotnet test backend/HansOS.slnx` |
| **Frontend Dev** | `cd frontend && pnpm dev:antd` |
| **Frontend Type Check** | `cd frontend && pnpm check:type` |

> There is no standalone lint command; do not invent one.

---

## Build / Run / Test

```bash
# Build backend
dotnet build backend/HansOS.slnx

# Run API server (Swagger: http://localhost:5180/swagger)
dotnet run --project backend/src/HansOS.Api

# Run all tests
dotnet test backend/HansOS.slnx

# Frontend dev server (port 5666)
cd frontend && pnpm install && pnpm dev:antd

# Frontend type check
cd frontend && pnpm check:type
```

---

## Testing Rules (Mandatory)

**Every new API endpoint and public method MUST have corresponding tests.**

| Change Type | Test Requirement |
|-------------|-----------------|
| New Controller endpoint | Must have corresponding Integration Test |
| New Service public method | Must have Unit Test or Integration Test |
| Modified endpoint behavior | Must update or add corresponding tests |
| Bug fix | Must have a test that reproduces the bug (red-green) |

### Test Project
- Integration Tests: `backend/tests/HansOS.Api.IntegrationTests/`
- Uses `WebApplicationFactory<Program>` for full API pipeline testing

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

## Pre-Task Requirements (Mandatory)

**BEFORE writing any code**, check if your task matches and read the required document:

| Task Keywords | MUST Read First |
|---------------|-----------------|
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.github/references/architecture.md` |
| EF Core, Migration, DbContext, Entity | `.github/references/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.github/references/vue-frontend.md` |
| UI, Styling, Component Appearance | `.github/references/ui-style-guide.md` |
| Review, Refactor, Quality | `.github/references/code-review-philosophy.md` |
| C#, Backend, Service, Controller | `.github/references/csharp-coding-style.md` |

---

## Architecture (Summary)

Monorepo with backend API + frontend SPA. Full details in `.github/references/architecture.md`.

**Key Patterns**: Features-based folders | JWT + HttpOnly Refresh Token | EF Core Code-First | API Envelope (`ApiEnvelope<T>`)

**Database**: PostgreSQL, EF Core Code-First. Migrations auto-applied on startup.

**Frontend**: Vue 3 Composition API + Ant Design Vue + Pinia + TypeScript strict mode

---

## EF Migration Rules

- Migrations are automatically applied on deployment via `Program.cs` (`MigrateAsync()`)
- No seed data classes — initial data is handled via migration `Up()` methods
- `__EFMigrationsHistory` table prevents duplicate execution
- Add migration: `dotnet ef migrations add <Name> --project backend/src/HansOS.Api`
- Apply locally: `dotnet ef database update --project backend/src/HansOS.Api`
- **Never** delete a migration that has been applied to any environment

---

## Deployment

- Push to `main` → GitHub Actions auto-deploy
- Backend → Azure App Service (auto-applies pending migrations on startup)
- Frontend → Azure Static Web Apps
- Single environment (personal project, no staging/production split)

---

## Workflow (Automated via Hooks + Task-based TDD)

Pipeline enforced by `.github/hooks/`. Code changes require feature branch + planning, actual execution is task-driven and test-first.

**Full workflow details → see `CLAUDE.md`**

```
接到需求 → 建立 Feature Branch → 腦力激盪 (optional) → 計畫模式 (required)
  → 依 phase 執行任務（TDD: RED → GREEN → REFACTOR）
  → 任務審查管線 → Commit
```

### Key Rules

- **Feature branch required**: `git checkout -b feature/<name>`
- **Plan Mode** required for code changes (3 parallel plan reviewers gate ExitPlanMode)
- **TDD**: RED → GREEN → REFACTOR for every task
- **Review Pipeline** (3 steps): Combined Code Review (code-simplifier + code-review-specialist + security-vuln-scanner, all parallel) → Linus Review → Build

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state |
| `workflow reset` | Reset all workflow state |
| `commit this` | Run full workflow + commit |
| `code-review` | Run review without commit |

### Skip Rules

- **Doc changes** (`.md`, `.txt`, `.yml`): Skip all workflow steps
- **Code changes**: Full pipeline required (plan + TDD + review + merge gate)

---

## Hooks System (Automatic)

Hooks run automatically. Defined in `.github/hooks/hooks.json`.

| Hook | Script | Function |
|------|--------|----------|
| `sessionStart` | `session-start.sh` | Initialize workflow, branch detection |
| `sessionEnd` | `session-end.sh` | Cleanup, progress log |
| `userPromptSubmitted` | `workflow-orchestrator.sh` | Detect commands, step completion, merge |
| `preToolUse` | `pre-edit-check.sh` | Main branch protection, file tracking |
| `preToolUse` | `pre-bash-check.sh` | Commit gating, merge gate |
| `postToolUse` | `post-edit-build.sh` | Auto-build, dependency restore |
| `postToolUse` | `post-tool-track.sh` | Track modifications, Smart Reset |
| `errorOccurred` | `error-handler.sh` | Error logging |

### Auto-enforced Rules
- **Main branch protection**: Cannot edit code files on `main`/`master`
- **Commit gating**: Code changes require all review steps complete
- **Protected files**: `.github/hooks/` cannot be modified
- **git add . blocked**: Stage specific files only
- **Auto-build**: `.cs` → `dotnet build`; `.vue`/`.ts` → `pnpm check:type`
- **Migration safety**: Cannot delete existing migration files

---

## Coding Standards

Detailed rules are in path-specific instructions (`.github/instructions/`) which auto-load based on file type.

### C# (auto-loaded when editing `backend/**/*.cs`)
- File-scoped namespaces, `_camelCase` private fields, `*Async` suffix
- `is null` / `is not null`, `string.Empty`, `record` for DTOs
- Methods ≤ 20 lines, nesting ≤ 3 levels

### Vue / TypeScript (auto-loaded when editing `frontend/**/*.{vue,ts,tsx}`)
- Composition API (`<script setup lang="ts">`), Options API forbidden
- Pinia, Ant Design Vue, Tailwind CSS, TypeScript strict mode

---

## Git Conventions

### Commits
- **Format**: Conventional Commits 1.0.0
- **Description language**: Traditional Chinese
- Example: `feat(auth): 新增 JWT refresh token 自動續期機制`

### Staging
- **Specific files only**, never `git add .` or `-A` (hook enforced)

### Branch Naming
- `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`

### Merge
- Always `--no-ff` (hook reminder)

---

## Communication Style

- Reply in **Traditional Chinese** (`zh-TW`) unless user explicitly requests another language
- Do not guess — use `[NEEDS CLARIFICATION]` when uncertain
- Do not bypass three-layer architecture
- See `.github/references/communication-style.md` for details

---

## Forbidden

- `git add .` — specific files only (hook blocked)
- Guessing — use `[NEEDS CLARIFICATION]`
- Bypassing three-layer architecture
- Hardcoding connection strings or JWT signing keys
