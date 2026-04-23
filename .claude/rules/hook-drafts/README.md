# Hook Drafts

這些是 Task Review Pipeline 重新設計需要的新 hook 檔案 drafts，因為 `pre-edit-check.mjs` 保護 `.claude/hooks/` 無法直接由 Claude 建立，因此放在這裡。

## 啟用方式

### 選項 A：一次全部複製
```bash
cp .claude/rules/hook-drafts/*.mjs .claude/hooks/
# 然後手動刪除 drafts
rm -rf .claude/rules/hook-drafts/
```

### 選項 B：分 phase 逐個複製（推薦）
跟著計畫 Phase 順序：

```bash
# Phase 0 Task 0.1
cp .claude/rules/hook-drafts/pre-exit-plan-check.mjs .claude/hooks/

# Phase 1.75 Task 1.75.1
cp .claude/rules/hook-drafts/pre-agent-gate-check.mjs .claude/hooks/

# Phase 4.75 Tasks 4.75.1-4.75.3
cp .claude/rules/hook-drafts/ledger-manager.mjs .claude/hooks/
cp .claude/rules/hook-drafts/workflow-gates.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-gate-validator.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-message-enhancer.mjs .claude/hooks/
```

## 每個新 hook 的對應 Phase

| Draft 檔 | Phase / Task | 類型 |
|---------|--------------|------|
| `pre-exit-plan-check.mjs` | Phase 0 Task 0.1 | PreToolUse(ExitPlanMode) hook |
| `pre-agent-gate-check.mjs` | Phase 1.75 Task 1.75.1 | PreToolUse(Agent) hook |
| `ledger-manager.mjs` | Phase 4.75 Task 4.75.1 | 共用模組（不直接當 hook） |
| `workflow-gates.mjs` | Phase 4.75 Task 4.75.2 | 共用模組（從 workflow-state 抽出） |
| `commit-gate-validator.mjs` | Phase 4.75 Task 4.75.3 | 共用模組（從 pre-bash-check 抽出） |
| `commit-message-enhancer.mjs` | Phase 4.75 Task 4.75.3 | 共用模組（從 pre-bash-check 抽出） |

## 既有 hook 的修改

參見 `MIGRATION.md`，列出每個既有 hook 要如何修改。
