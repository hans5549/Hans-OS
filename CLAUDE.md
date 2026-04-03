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

Pipeline enforced by hooks. Not all changes need all phases — see skip rules below.

```
接到需求 → 建立 Worktree → 腦力激盪 (optional) → 計畫模式 (required)
  → 實作程式碼 → 審查管線 → 合併回主線 → 刪除 Worktree
```

### Git Worktree (Required for Code Changes)

程式變更**必須**在 git worktree 上開發，不可直接在 main 上作業。

```bash
# 建立 feature branch 的 worktree
git worktree add ../Hans-OS-<branch-name> -b <branch-name>
cd ../Hans-OS-<branch-name>
```

- 分支命名：`feature/add-xxx`、`fix/xxx-error`、`refactor/xxx`
- Worktree 放在上層目錄，避免巢狀
- 建立後自動執行 `dotnet restore` + `pnpm install`（hook 強制）

### Phase 1: Brainstorming (Skill, optional)

Trigger: `/brainstorming` or when starting a new feature idea.
Output: Design spec in `.claude/specs/`.
Model: `claude-opus-4.6`

### Phase 2: Plan Mode (Hard Gate, **required** for code changes)

Claude Code's built-in plan mode. System prevents code edits. Plan review agents validate before exit.

| Step | Agent | Model |
|------|-------|-------|
| CEO Review | `plan-ceo-reviewer` | `claude-opus-4.6` |
| Eng Review | `plan-eng-reviewer` | `claude-opus-4.6` |
| Plan Linus Review | `plan-linus-reviewer` | `gpt-5.4` |

Dispatch all 3 **in parallel** (one message, multiple Agent tool calls).
`pre-exit-plan-check` hook blocks ExitPlanMode until all 3 complete.

**Review Conflict Resolution**: When CEO review and Linus review conflict (e.g., CEO suggests expansion, Linus flags over-engineering), do NOT auto-resolve. Present both positions to the user for decision.

### Phase 3: Coding Phase (hooks enforced)

```
Simplifier (gpt-5.4) → [Code Review + Security] parallel (gpt-5.4) → Linus (gpt-5.4) → Build → Commit
```

| Step | Agent | Model | Completion Signal |
|------|-------|-------|-------------------|
| Code Simplifier | `code-simplifier:code-simplifier` | `gpt-5.4` | auto-completed by hook |
| Code Review | `code-review-specialist` | `gpt-5.4` | auto-completed by hook |
| Security Review | `security-vuln-scanner` | `gpt-5.4` | auto-completed by hook |
| Linus Review | `linus-reviewer` | `gpt-5.4` | after CR + Security |
| Build | Auto-verified on commit | — | Automatic |

**Dispatch pattern**: After simplifier completes, dispatch Code Review + Security **in parallel** (one message, multiple Agent tool calls). After both complete, dispatch Linus Review. Do not dispatch sequentially.

### Phase 4: Merge Back to Main

開發完成並通過所有審查後，使用 `merge this` 觸發合併流程：

1. 先把 main 合併到 feature branch，確保沒有衝突
2. `dotnet build` + `dotnet test` + `pnpm check:type`
3. 確認通過 → 切回 main 執行 `git merge --no-ff`
4. `git worktree remove` + `git branch -d`

### Skip Rules (Binary)

| 變更類型 | 定義 | Plan Mode | Plan Review | Coding Phase |
|----------|------|:---------:|:-----------:|:------------:|
| 文字變更 | Doc extensions (`.md`, `.txt`, `.rst`, `.yml`, `.yaml`) | Skip | Skip | Skip |
| 程式變更 | Code extensions (`.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`) | Required | All 3 (CEO + Eng + Linus) | All (Simplifier + Code Review + Security + Linus + Build) |

No file-count tiers. Any code change = full pipeline.

### Smart Reset Rules

- **Simplifier exemption**: After simplifier completes, subsequent code edits only reset post-simplifier steps (Code Review, Security, Linus), NOT simplifier itself.
- **Small change tolerance**: Cumulative edits < 10 lines after review → warning only, reviews preserved. >= 10 lines → reset.

### Agent Dispatch Rules (MANDATORY)

- You MUST use the Agent tool to dispatch. Do NOT substitute with your own text analysis.
- The `post-agent-verify` hook verifies Agent tool was actually called — text summaries will NOT mark steps complete.
- All review agents produce: **concise summary** (max 300 tokens) to main conversation + **full report** to `.claude/reviews/`.
- Use `/commit-this` or `/review-workflow` slash commands for guided workflow execution.

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state and pending steps (含 worktree 資訊) |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `code-review` | Run full review workflow without commit |
| `commit this` | Run full workflow and create git commit |
| `merge this` | Merge feature branch back to main and cleanup worktree |
| `/commit-this` | Full workflow with guided Agent dispatch + git commit |
| `/review-workflow` | Full review workflow without commit |
| `/merge-this` | Guided merge flow |
| `/brainstorming` | Start interactive brainstorming session |

### Workflow Rules

- **Build FAIL** → fix → Build is automatically re-verified
- **Code file edits** automatically reset review steps (with smart exemptions above)
- Planning steps (CEO/Eng/Linus) auto-reset each new session
- **Branch-scoped state**: Workflow state keyed by branch name — parallel worktrees have independent state

### Tracked File Types

Code: `.cs`, `.razor`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`, `.vue`, `.ts`, `.tsx`
Doc: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` (skip build verification)

### Auto-Enforcement Rules

| Rule | Description |
|------|-------------|
| Main branch protection | Cannot edit code files on `main`/`master` |
| Worktree required | Code changes must be on a git worktree (session start detection) |
| Merge gate | `git merge` to main requires all workflow steps complete + main merged to feature |
| Protected files | `.github/hooks/` and workflow state files cannot be modified |
| git add . blocked | Must stage specific files only |
| Commit gating | Code changes require all review steps complete before commit |
| Auto-build | `.cs` edits → `dotnet build`; `.vue`/`.ts` edits → `pnpm check:type` |
| Build strike | 3 fails → warning, 5 fails → stop and report |
| Auto-setup | `git worktree add` → auto `dotnet restore` + `pnpm install` |
| Dependency auto-restore | `.csproj` edit → `dotnet restore`; `package.json` edit → `pnpm install` |
| Migration safety | Cannot delete existing migration files; PascalCase naming enforced |

---

## Build & Verification

- After modifying .cs files -> `dotnet build --no-restore -v q`
- After modifying .vue/.ts/.tsx files -> `cd frontend && pnpm check:type`
- Build Self-Healing: max 5 retries, then stop and report (enforced by hook)
- DLL lock -> retry with `--no-incremental`, then tell user which process to close

### Related Skills
- `/commit-this` — Execute full workflow and commit
- `/review-workflow` — Execute review workflow without commit
- `/execute-plan` — Execute a plan immediately
- `/refactor-plan` — Phased refactoring
- `/safe-cleanup` — Safe code cleanup

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
