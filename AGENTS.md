# AGENTS.md — Hans-OS

本檔是 `Codex` 在 `Hans-OS` repo 的第一入口。
`Claude` 仍使用既有 `CLAUDE.md` 與 `.claude/*`；`Codex` 請優先依本檔與 `.Codex/*` 工作。

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

## Communication

- **回覆語言一律使用繁體中文（zh-TW）**。
- 語氣保持直接、務實、可執行。
- 不確定時必須明講假設或缺口，不可猜。

## Codex Must-Read Mapping

在開始修改前，先依任務性質閱讀對應文件：

| Task Keywords | MUST Read First |
|---------------|-----------------|
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.Codex/ARCHITECTURE.md` |
| EF Core, Migration, DbContext, Entity | `.Codex/rules/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.Codex/rules/review-vue.md` |
| Review, Refactor, Quality | `.Codex/LINUS_MODE.md` |
| Workflow, Plan, Commit, Review Pipeline | `.Codex/rules/workflow.md` |
| Communication, Response Style | `.Codex/rules/communication-style.md` |

## Workflow

### Binary Rules

- **Doc-only changes**（`.md`, `.txt`, `.rst`, `.yml`, `.yaml`）可跳過 TDD、build、review pipeline。
- **Code changes**（`.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`）必須走完整工作流。

### Required for Code Changes

1. 先進入 **plan mode**，將需求拆成可執行步驟。
2. 在 **feature / fix / refactor branch** 工作，不可直接在 `main` / `master` 修改程式碼。
3. 依序執行 **RED → GREEN → REFACTOR**。
4. 完成後跑對應驗證與 review pipeline。

### Branch Rules

- 分支命名：`feature/add-xxx`、`fix/xxx-error`、`refactor/xxx`
- `Codex` 雖可建立 `codex/*` 分支，但本 repo 以專案規範為主，**優先使用上述命名**。

### Git Rules

- **禁止 `git add .` 與 `git add -A`**
- 僅能 stage 具體檔案路徑。
- Commit message 採 Conventional Commits，內容使用繁體中文。

## TDD and Tests

**Every new API endpoint and public method MUST have corresponding tests.**

| Change Type | Test Requirement |
|-------------|-----------------|
| New Controller endpoint | Must have corresponding Integration Test |
| New Service public method | Must have Unit Test or Integration Test |
| Modified endpoint behavior | Must update or add corresponding tests |
| Bug fix | Must have a test that reproduces the bug (red-green) |

### Minimum Coverage Per Endpoint

1. Happy path
2. `401 Unauthorized`
3. `400 Bad Request`（若 applicable）
4. Business logic edge cases

## Verification Commands

### Backend

```bash
dotnet build backend/HansOS.slnx
dotnet test backend/HansOS.slnx
```

### Frontend

```bash
cd frontend && pnpm check:type
```

- `.cs` / backend behavior 變更：至少跑 backend build 與相關 tests。
- `.vue` / `.ts` / `.tsx` 變更：至少跑 `pnpm check:type`。
- 若同時影響前後端，兩邊都要驗證。

## Review Pipeline

程式碼變更完成後，應手動遵守以下 review 順序：

1. Plan Review：`plan-ceo-reviewer`、`plan-eng-reviewer`、`plan-linus-reviewer` 平行檢查
2. Code Review：`code-review-specialist`、`security-vuln-scanner` 平行檢查
3. Linus Review：`linus-reviewer`
4. Build / Typecheck / Tests

Reviewer persona 與輸出契約見 `.Codex/agents/*`。

## Codex vs Claude

- `Claude` 版的 branch protection、commit gate、post-edit build、agent verification，由 `.claude/hooks/*` 自動執行。
- `Codex` **不提供等價 hook enforcement**；請依 `.Codex/rules/workflow.md` 手動遵守相同規範。
- `CLAUDE.md` 保留給 `Claude Code` 使用；`Codex` 不應修改 `.claude/hooks/*`、`.claude/workflow/state.json` 等 Claude 自動化檔案，除非使用者明確要求處理 Claude workflow 本身。
