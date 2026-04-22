# Workflow — Claude to Codex Mapping

本文件將 `Hans-OS/.claude/hooks/*`、`.claude/commands/*`、`.claude/workflow/*` 的實際自動化行為，翻譯成 `Codex` 可遵守的人工工作流。

## Core Principle

- `Claude Code`：有 hooks、workflow state、agent verification，可做自動 gate
- `Codex`：沒有與上述能力完全等價的 repo-local hook enforcement
- 因此在 `Codex` 端，這些規則屬於 **必須遵守的人工規範**，不是自動攔截

## Change Type Rules

| Change Type | Definition | Plan Mode | TDD | Review Pipeline | Build / Typecheck |
|-------------|------------|:---------:|:---:|:---------------:|:-----------------:|
| Doc-only | `.md`, `.txt`, `.rst`, `.yml`, `.yaml` | Skip | Skip | Skip | Skip |
| Code change | `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` | Required | Required | Required | Required |

## Required Flow for Code Changes

1. **確認不在 `main` / `master` 上直接改 code**
2. **進 plan mode**
3. **選定當前 task**
4. **RED**：先寫失敗測試或先補重現 bug 的測試
5. **GREEN**：寫最小程式讓測試通過
6. **REFACTOR**：整理命名、消除重複、降低複雜度
7. **Review pipeline**
8. **Build / Typecheck / Tests**
9. **只 stage 指定檔案後 commit**

## Manual Rules in Codex

### Branch Protection

Claude 來源：

- `.claude/hooks/pre-edit-check.mjs`

Claude 會做的事：

- 若在 `main` / `master` 編輯程式碼，自動阻擋
- 保護 `.claude/hooks/*`、`.claude/workflow/state.json`、`.claude/settings.local.json`

Codex 對應規範：

- 程式碼修改前，手動確認目前 branch 不是 `main` / `master`
- 不修改 Claude workflow 相關檔案，除非需求本身就是維護 Claude automation

### Commit Gate

Claude 來源：

- `.claude/hooks/pre-bash-check.mjs`

Claude 會做的事：

- 在 `git commit` 前檢查 review steps 是否完成
- 檢查是否有 backend build / frontend typecheck
- 阻止 `git add .` / `git add -A`

Codex 對應規範：

- commit 前自行核對 review、build、test、typecheck 是否完成
- 僅能 `git add <specific-files>`

### Post-Edit Build

Claude 來源：

- `.claude/hooks/post-edit-build.mjs`

Claude 會做的事：

- `.cs` / `.razor` 修改後自動 `dotnet build backend/HansOS.slnx --no-restore -v q`
- `.vue` / `.ts` / `.tsx` 修改後自動 `cd frontend && pnpm check:type`

Codex 對應規範：

- backend code 變更後，手動跑 backend build
- frontend code 變更後，手動跑 `pnpm check:type`
- 若雙邊都改，兩邊都跑

### Agent Verification

Claude 來源：

- `.claude/hooks/post-agent-verify.mjs`

Claude 會做的事：

- 只有真的 dispatch agent，workflow step 才算完成

Codex 對應規範：

- 若要聲稱已完成 review pipeline，必須真的執行對應 reviewer / sub-agent，不可只用主 agent 自述替代
- `.Codex/agents/*` 是 reviewer persona 與 prompt 契約來源

## Review Pipeline

### Plan Review

平行執行：

- `plan-ceo-reviewer`
- `plan-eng-reviewer`
- `plan-linus-reviewer`

若 CEO review 與 Linus review 衝突，不自動裁決，交由使用者決定。

### Code Review

平行執行：

- `code-review-specialist`
- `security-vuln-scanner`

之後再執行：

- `linus-reviewer`

## Command Mapping

Claude 既有命令：

- `.claude/commands/commit-this.md`
- `.claude/commands/review-workflow.md`
- `.claude/commands/review-vue.md`

Codex 對應方式：

- 不建立假 slash commands
- 使用本文件與 `.Codex/agents/*` 作為人工操作說明
- 主 agent 在需要時主動 dispatch reviewer，並在回覆中說明目前處於哪個 workflow step

## Build / Test Matrix

| Change | Required Validation |
|--------|---------------------|
| Backend `.cs` / API behavior | `dotnet build backend/HansOS.slnx` + relevant tests |
| Frontend `.vue` / `.ts` / `.tsx` | `cd frontend && pnpm check:type` |
| New / changed API endpoint | Integration tests |
| New public service method | Unit or Integration tests |
| Bug fix | Reproduction test first |

## Work Artifacts

- `Codex` 端不鏡像 `.claude/workflow/state.json`、`progress.md`、`findings.md` 這類機器狀態檔
- 若需要工作痕跡，以一般文件或回覆摘要呈現，不建立假 state machine

## What This File Does Not Claim

- 不宣稱 `Codex` 會自動擋 branch、commit 或 edit
- 不宣稱 `Codex` 會自動在編輯後 build
- 不宣稱 `Codex` 會自動驗證 reviewer 是否真的執行

這些都是 **人工必守規範**，不是工具層 enforcement
