# CLAUDE.md — Hans-OS

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
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.claude/ARCHITECTURE.md` |
| Edit *.cs, Write *.cs, C#, class, method, async, LINQ, record | Auto-loaded (`~/.claude/rules/csharp-coding-style.md`) |
| EF Core, Migration, DbContext, Entity | `.claude/rules/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.claude/rules/review-vue.md` (when available) |
| Review, Refactor, Quality | `.claude/LINUS_MODE.md` |

---

## Architecture (Summary)

Monorepo with backend API + frontend SPA. Full details in `.claude/ARCHITECTURE.md`.

**Key Patterns**: Features-based folders | JWT + HttpOnly Refresh Token | EF Core Code-First | API Envelope (`ApiEnvelope<T>`)

**Database**: PostgreSQL, EF Core Code-First. Migrations auto-applied on startup.

**Frontend**: Vue 3 Composition API + Ant Design Vue + Pinia + TypeScript strict mode

> Architecture details (project structure, auth flow, API patterns) -> see `.claude/ARCHITECTURE.md`

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

## Workflow (Automated)

The development workflow is **enforced by hooks**. All steps must be completed before commit:

```
Planner (optional) -> Code -> Simplifier -> Spec Check -> Code Review -> Security -> Linus -> Build -> Commit
```

### Required Steps

| Step | Tool/Agent | Completion Signal |
|------|------------|-------------------|
| 1. Code Simplifier | `code-simplifier:code-simplifier` agent | Say "simplifier done" |
| 1.5 Spec Check | Compare implementation vs requirements | Say "spec check done" |
| 2. Code Review | `code-review-specialist` agent | Say "code review done" |
| 3. Security Review | `security-vuln-scanner` agent | Say "security review done" |
| 4. Linus Review | Apply `.claude/LINUS_MODE.md` | Say "Linus Green" |
| 5. Build | Auto-verified on commit | Automatic |

### Agent Dispatch Rules (MANDATORY)

When workflow requires running agents (simplifier, code-review, security):
- You MUST use the Agent tool to dispatch. Do NOT substitute with your own text analysis.
- The `post-agent-verify` hook verifies Agent tool was actually called -- text summaries will NOT mark steps complete.
- Use `/commit-this` or `/review-workflow` slash commands for guided workflow execution.

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state and pending steps |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `code-review` | Run full review workflow without commit |
| `commit this` | Run full workflow and create git commit |
| `/commit-this` | Full workflow with guided Agent dispatch + git commit |
| `/review-workflow` | Full review workflow without commit |

### Workflow Rules

- **Build FAIL** -> fix -> Build is automatically re-verified
- **Code file edits** automatically reset review steps
- Spec Check: re-read requirements, verify completeness, check YAGNI

### Tracked File Types

Code: `.cs`, `.razor`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`, `.vue`, `.ts`, `.tsx`, `.mts`
Doc: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` (skip build verification)

---

## Build & Verification

- After modifying .cs files -> `dotnet build --no-restore -v q`
- After modifying .vue/.ts/.tsx files -> `cd frontend && pnpm check:type`
- Build Self-Healing: max 5 retries, then stop and report (enforced by hook)
- DLL lock -> retry with `--no-incremental`, then tell user which process to close

### Related Skills
- `/commit-this` -- Execute full workflow and commit
- `/review-workflow` -- Execute review workflow without commit

---

## Coding Standards

Full guide: `~/.claude/rules/csharp-coding-style.md` (auto-loaded)

**Commits**: Conventional Commits 1.0.0, descriptions in Traditional Chinese (enforced by hook)

### Git

- **Staging**: Specific files only, never `git add .` or `-A` (enforced by hook)
- **Branch**: `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`
- **Merge**: Always `--no-ff` (reminded by hook)

### Forbidden

- `git add .` -- specific files only (hook blocked)
- Guessing -- use `[NEEDS CLARIFICATION]`
- Bypassing three-tier architecture

---

## Communication Style

See `.claude/rules/communication-style.md`
