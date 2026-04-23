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

Claude Code's built-in plan mode. System prevents code edits. **4 plan reviewers** validate before exit.

| Step | Agent | Model |
|------|-------|-------|
| CEO Review | `plan-ceo-reviewer` | `claude-opus-4.6` |
| Eng Review | `plan-eng-reviewer` | `claude-opus-4.6` |
| Plan Linus Review | `plan-linus-reviewer` | `gpt-5.4` |
| **Plan Codex Adversarial Review** | `plan-codex-adversarial-reviewer`（wrapper） | `sonnet` (wrapper) + `gpt-5.4` (Codex actual) |

Dispatch all **4 in parallel** (single message, 4 Agent tool calls).
`pre-exit-plan-check` hook blocks ExitPlanMode until:
- All 4 reviewers complete
- `planCodexVerdict !== "needs-attention"`
- All Plan Ledgers (`.claude/workflow/ledger-plan-*.md`) have MEDIUM+ findings disposed (FIXED/INCORPORATED/DISMISSED/DEFERRED)

**Review Conflict Resolution**: When reviewers conflict (e.g., CEO suggests expansion, Linus flags over-engineering, Codex raises unseen failure mode), do NOT auto-resolve. Present all positions to the user for decision.

**Plan Codex Degradation**: 若 Codex wrapper 回傳 verdict=`DEGRADED`（未登入/timeout/quota），使用 `workflow override planCodex <reason>` 跳過（reason ≥ 20 字，記入 skip-log.md）。

### Phase 3: Task Execution (TDD required)

每個程式任務都必須依目前 phase 順序完成，不可跳到後續 phase，也不可把多個需求混在同一個 commit。

1. 從 `plan.md`、track plan 或目前工作清單中選出當前 phase 的下一個 pending task
2. 先把 task 標成進行中（例如 `[~]` 或 `in_progress`），再開始修改程式
3. **RED**：先寫會失敗的測試。Bug fix 必須先重現 bug；重構必須先補 characterization tests
4. **GREEN**：只寫讓測試通過所需的最小程式碼
5. **REFACTOR**：在維持綠燈的前提下整理命名、抽取重複、降低巢狀與方法長度
6. **Task Verification**：相關測試必須通過；若工具鏈支援，目標是變更模組覆蓋率 >= 80%；任何偏離計畫、額外依賴或 scope 變動都要回寫計畫

只有純文件、純註解、純環境設定且不改 runtime 行為的變更，才可跳過 RED/GREEN；若是行為變更，必須有測試。

### Phase 4: Task Review Pipeline (hooks enforced, mission-based gates)

```
Gate A (Safety) → Gate B (Project Fit) → Gate C (Taste) → Gate D (Cleanup) → Gate X (Cross-Model) → Build → stop-review-gate → Task Commit
```

每個 gate 依序執行，**前一關 findings 全部 disposition 才能 dispatch 下一關**（`pre-agent-gate-check` hook 強制）。

| Gate | Mission | Agent | Model |
|------|---------|-------|-------|
| **Gate A** | Safety（OWASP 專精） | `security-vuln-scanner` | `claude-opus-4.6` |
| **Gate B** | Project Fit（Hans-OS 架構/spec/慣例） | `code-review-specialist` | `claude-opus-4.6` |
| **Gate C** | Taste & Back-compat（Linus 品味） | `linus-reviewer` | `claude-opus-4.6` |
| **Gate D** | Post-AI Cleanup（簡化） | `code-simplifier` | `sonnet` |
| **Gate X** | Cross-Model Verification（跨模型） | `gatex-codex-reviewer`（wrapper） | `sonnet` (wrapper) + `gpt-5.4` (Codex actual) |
| Build | 自動 build 驗證 | — | Automatic |
| stop-review-gate | Codex verdict 最終 safety net | OpenAI Codex | `gpt-5.4` |

**Dispatch pattern**:
- 依序 dispatch 單一訊息一個 agent（不是平行）
- 每關完成後必須處理 Findings Ledger 的 MEDIUM+ findings 才能進下一關
- Gate X 必須在 Gate A-D 全完成後才能 dispatch（`pre-agent-gate-check` 強制）

**Findings Ledger 機制**：每個 agent 跑完自動生成 `.claude/workflow/ledger-{gate}-*.md`。主 Claude 對 MEDIUM+ findings 選擇 disposition：
- `FIXED` + commit hash / file reference
- `DISMISSED` + reason（false positive / not applicable）
- `DEFERRED` + 連結到 `.claude/workflow/deferred.md#entry-{id}`
- LOW/INFO 自動 DISMISSED

`pre-bash-check` 在 commit 時檢查所有 ledger disposition；commit message 自動帶 `Ledger-Refs:` 欄位。

**Override 逃生閥**：`workflow override <target> <reason>` 可跳過某個 gate 的完成要求（target: gateA/gateB/gateC/gateD/gateX/planCodex/unblock-next；reason ≥ 20 字；記入 skip-log.md；commit message 強制含 `Override-{target}-Reason:`）。

**Task commit rule**: 單一 task 完成後才能 commit；commit 前要確認 TDD 步驟、5 個 gate、Ledger disposition、build / typecheck / tests 都已完成，且 commit 內容只涵蓋目前 task。

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
| `workflow status` | View current state, pending steps, ledger progress, deferred count |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `workflow override <target> <reason>` | 跳過特定 gate / plan reviewer 的完成要求（reason ≥ 20 字） |
| `code-review` | Run full review workflow for current task without commit |
| `commit this` | Run full workflow for current task and create git commit |

### Workflow 檔案時間軸分工

| 檔案 | 時間軸 | 寫入 | 讀取 |
|------|-------|------|------|
| `.claude/workflow/progress.md` | **過去**（session 總結） | `on-stop.mjs` | `pre-compact.mjs` |
| `.claude/workflow/ledger-*.md` | **現在**（當前 task findings） | `post-agent-verify.mjs` | `pre-exit-plan-check` / `pre-agent-gate-check` / `pre-bash-check` |
| `.claude/workflow/deferred.md` | **未來**（待處理 TODO） | 主 Claude（via Ledger disposition） | `pre-compact.mjs` / `workflow status` |
| `.claude/workflow/skip-log.md` | override / skip 留痕 | workflow-orchestrator | 人工審計 |

### Codex stop-review-gate 警示

Pipeline 末端啟用了 OpenAI Codex 的 `stop-review-gate`——Claude 每次 Stop 時會觸發 Codex 對前一輪編輯做 targeted review，verdict=needs-attention 時會擋 Stop 直到問題處理完。

**⚠️ 官方警告**：此 gate 會引發 Claude↔Codex loop，可能快速耗用 OpenAI API quota。**只建議在主動監控的 session 啟用**。

**啟用**：`/codex:setup --enable-review-gate`
**緊急關閉**：`/codex:setup --disable-review-gate`

**費用監控**：定期檢查 OpenAI dashboard。每日 Hans-OS 開發估計 $1-5 USD。若異常偏高，先關閉 gate 排查。

詳見 `.claude/rules/codex-integration.md`。

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
