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

## Workflow (Automated + Task-based TDD)

Pipeline enforced by hooks. Code changes require feature branch + plan mode, actual execution is task-driven and test-first.

```
接到需求 → 建立 Feature Branch → 腦力激盪 (optional) → 計畫模式 (required)
  → 依 phase 執行任務（TDD: RED → GREEN → REFACTOR）
  → 任務審查管線 → Commit
```

### Git Branch (Required for Code Changes)

程式變更**不可**直接在 main 上作業，必須建立 feature branch。

```bash
git checkout -b feature/<name>
```

- 分支命名：`feature/add-xxx`、`fix/xxx-error`、`refactor/xxx`

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

### Phase 3: Task Execution (TDD required)

每個程式任務都必須依目前 phase 順序完成，不可跳到後續 phase，也不可把多個需求混在同一個 commit。

1. 從 `plan.md`、track plan 或目前工作清單中選出當前 phase 的下一個 pending task
2. 先把 task 標成進行中（例如 `[~]` 或 `in_progress`），再開始修改程式
3. **RED**：先寫會失敗的測試。Bug fix 必須先重現 bug；重構必須先補 characterization tests
4. **GREEN**：只寫讓測試通過所需的最小程式碼
5. **REFACTOR**：在維持綠燈的前提下整理命名、抽取重複、降低巢狀與方法長度
6. **Task Verification**：相關測試必須通過；若工具鏈支援，目標是變更模組覆蓋率 >= 80%；任何偏離計畫、額外依賴或 scope 變動都要回寫計畫

只有純文件、純註解、純環境設定且不改 runtime 行為的變更，才可跳過 RED/GREEN；若是行為變更，必須有測試。

### Phase 4: Task Review Pipeline (hooks enforced)

```
[Combined Code Review] all 3 parallel (gpt-5.4) → Linus (gpt-5.4) → Build → Task Commit
```

| Step | Agents | Model | Completion Signal |
|------|--------|-------|-------------------|
| Combined Code Review | `code-simplifier` + `code-review-specialist` + `security-vuln-scanner` | `gpt-5.4` | auto-completed by hook (any of the 3 agents marks step done) |
| Linus Review | `linus-reviewer` | `gpt-5.4` | after Combined Code Review |
| Build | Auto-verified on commit | — | Automatic |

**Dispatch pattern**: Dispatch all 3 Code Review agents **in parallel** (one message, multiple Agent tool calls). After all complete, dispatch Linus Review. Do not dispatch sequentially.

**Task commit rule**: 單一 task 完成後才能 commit；commit 前要確認 TDD 步驟、review pipeline、build / typecheck / tests 都已完成，且 commit 內容只涵蓋目前 task。

### Skip Rules (Binary)

| 變更類型 | 定義 | Plan Mode | Plan Review | Task TDD | Review Pipeline |
|----------|------|:---------:|:-----------:|:--------:|:---------------:|
| 文字變更 | Doc extensions (`.md`, `.txt`, `.rst`, `.yml`, `.yaml`) | Skip | Skip | Skip | Skip |
| 程式變更 | Code extensions (`.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`) | Required | All 3 (CEO + Eng + Linus) | Required | All (Combined Review + Linus + Build) |

No file-count tiers. Any code change = full pipeline.

### Smart Reset Rules

- **Small change tolerance**: Cumulative edits < 10 lines after Combined Code Review → warning only, reviews preserved. >= 10 lines → reset all coding review steps.
- **Code file edits** automatically reset review steps (with tolerance above)

### Agent Dispatch Rules (MANDATORY)

- You MUST use the Agent tool to dispatch. Do NOT substitute with your own text analysis.
- The `post-agent-verify` hook verifies Agent tool was actually called — text summaries will NOT mark steps complete.
- All review agents produce: **concise summary** (max 300 tokens) to main conversation + **full report** to `.claude/reviews/`.

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state, pending steps, current task |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `code-review` | Run full review workflow for current task without commit |
| `commit this` | Run full workflow for current task and create git commit |

### Workflow Rules

- **Build FAIL** → fix → Build is automatically re-verified
- **Code file edits** automatically reset review steps (with smart tolerance above)
- Planning steps (CEO/Eng/Linus) auto-reset each new session
- **Task-scoped commits**: 一個 commit 只解決一個 task；若變更跨多個 task，先拆分再提交

### Tracked File Types

Code: `.cs`, `.razor`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`, `.vue`, `.ts`, `.tsx`
Doc: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` (skip build verification)

### Auto-Enforcement Rules

| Rule | Description |
|------|-------------|
| Main branch protection | Cannot edit code files on `main`/`master` |
| Protected files | `.github/hooks/` and workflow state files cannot be modified |
| git add . blocked | Must stage specific files only |
| Commit gating | Code changes require all review steps complete before commit |
| Auto-build | `.cs` edits → `dotnet build`; `.vue`/`.ts` edits → `pnpm check:type` |
| Build strike | 3 fails → warning, 5 fails → stop and report |
| Dependency auto-restore | `.csproj` edit → `dotnet restore`; `package.json` edit → `pnpm install` |
| Migration safety | Cannot delete existing migration files; PascalCase naming enforced |

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

- **回覆語言**：一律使用繁體中文 (zh-TW)
- See `.claude/rules/communication-style.md`
