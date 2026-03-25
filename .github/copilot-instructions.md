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

## Workflow (Automated via Hooks)

Three-phase pipeline enforced by `.github/hooks/`. Not all changes need all phases.

```
Brainstorming (optional) → Planning (optional) → Coding Phase → Commit
```

### Phase 1: Brainstorming (optional)

Invoke: `@brainstorming`
Output: Design spec document.

### Phase 2: Planning (optional)

Invoke: `@planning-reviewer`
Three-perspective review: CEO (scope), Engineering (execution), Linus (simplicity).

### Phase 3: Coding Phase (hooks enforced)

```
Code Simplifier → Code Review → Security Review → Linus Review → gstack Review → Build & Commit
```

| Step | Agent | Purpose |
|------|-------|---------|
| 1 | `@code-simplifier` | Simplify code (primary constructors, guard clauses, etc.) |
| 2 | `@code-review` | Code quality, architecture compliance, naming |
| 3 | `@security-scanner` | OWASP Top 10, injection, auth bypass |
| 4 | `@linus-reviewer` | Good Taste, backward compatibility, simplicity |
| 5 | `@gstack-reviewer` | SQL safety, race conditions, trust boundary |
| 6 | Build & Commit | `dotnet build` + `pnpm check:type` + `dotnet test` + git commit |

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state and pending steps |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `commit this` | Trigger full commit workflow |
| `review this` / `code-review` | Run full review without commit |

### Workflow Agents

| Agent | Description |
|-------|-------------|
| `@commit-workflow` | Full 6-step commit pipeline (guided execution) |
| `@review-workflow` | Full review pipeline without commit |

### Skip Rules

| Change Type | Definition | Coding Phase |
|------------|------------|:------------:|
| 文字變更 | `.md`, `.txt`, `.rst`, `.yml`, `.yaml` | Skip (hooks auto-detect) |
| 程式變更 | `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` | Full pipeline |

### Smart Reset Rules

- **Simplifier exemption**: After simplifier completes, subsequent code edits only reset post-simplifier steps (Code Review, Security), NOT simplifier itself.
- **Small change tolerance**: Cumulative edits < 10 lines after review → warning only, reviews preserved. >= 10 lines → reset.

---

## Hooks System (Automatic)

Hooks run automatically and enforce workflow rules. Defined in `.github/hooks/hooks.json`.

| Hook | Script | Function |
|------|--------|----------|
| `sessionStart` | `session-start.sh` | Initialize workflow, show status |
| `sessionEnd` | `session-end.sh` | Cleanup, write progress log |
| `userPromptSubmitted` | `workflow-orchestrator.sh` | Detect commands, step completion |
| `preToolUse` | `pre-edit-check.sh` | Main branch protection, file protection |
| `preToolUse` | `pre-bash-check.sh` | Block `git add .`, commit gating |
| `postToolUse` | `post-edit-build.sh` | Auto-build after code edits |
| `postToolUse` | `post-tool-track.sh` | Track modifications, Smart Reset |
| `errorOccurred` | `error-handler.sh` | Error logging |

### Auto-enforced Rules
- **Main branch protection**: Cannot edit code files on `main`/`master` (workflow files OK)
- **Protected files**: `.github/hooks/` and `.github/workflow/state.json` cannot be modified
- **git add . blocked**: Must stage specific files only
- **Commit gating**: Code changes require all workflow steps complete before `git commit`
- **Auto-build**: `.cs` edits trigger `dotnet build`, `.vue`/`.ts` edits trigger `pnpm check:type`
- **Build strike system**: 3 fails → warning, 5 fails → stop and report

---

## Build & Verification

- After modifying .cs files → `dotnet build --no-restore -v q` (auto by hook)
- After modifying .vue/.ts/.tsx files → `cd frontend && pnpm check:type` (auto by hook)
- Build Self-Healing: max 5 retries, then stop and report

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
