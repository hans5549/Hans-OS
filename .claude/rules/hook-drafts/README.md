# Hook Drafts — 完整替換版

這些是 Task Review Pipeline 重新設計需要的 hook 檔 drafts。`pre-edit-check.mjs` 保護 `.claude/hooks/`，Claude 無法直接編輯——改為把完整新版放這裡，使用者 `cp` 覆蓋即可。

**全部 14 個 hook drafts 語法已通過 `node --check`**。

---

## 一次全量部署（推薦）

```bash
# 備份現有 hooks
cp -r .claude/hooks .claude/hooks.backup-$(date +%Y%m%d-%H%M)

# 複製所有新版
cp .claude/rules/hook-drafts/*.mjs .claude/hooks/

# 刪除原本 dead file
rm .claude/workflow/findings.md

# 手動編輯 settings.local.json 加兩條 registration（見 settings.local.json.snippet.md）
#   PreToolUse / Agent → pre-agent-gate-check.mjs
#   PreToolUse / ExitPlanMode → pre-exit-plan-check.mjs

# 驗證
for f in hook-utils workflow-state workflow-gates ledger-manager \
         workflow-orchestrator pre-edit-check pre-bash-check \
         commit-gate-validator commit-message-enhancer \
         pre-exit-plan-check pre-agent-gate-check \
         post-edit-build post-agent-verify \
         session-start on-stop pre-compact; do
  node --check ".claude/hooks/${f}.mjs" && echo "OK: $f"
done

# 測試 schema 遷移
node -e "
  import('./.claude/hooks/workflow-state.mjs').then(m => {
    m.setWorkflowState({ completedSteps: { codeReview: true, linusReview: true } });
    const s = m.getWorkflowState();
    console.log('schemaVersion:', s.schemaVersion);
    console.log('gateSafetyDone:', s.completedSteps.gateSafetyDone);
    console.log('gateProjectFitDone:', s.completedSteps.gateProjectFitDone);
    console.log('gateTasteDone:', s.completedSteps.gateTasteDone);
    console.log('gateCleanupDone:', s.completedSteps.gateCleanupDone);
  });
"

# 清理 drafts
rm -rf .claude/rules/hook-drafts/
```

---

## Drafts 清單

### 新建檔案（6 個）

| 檔案 | 對應 Phase / Task | 類型 |
|------|-------------------|------|
| `pre-exit-plan-check.mjs` | Phase 0 Task 0.1 | 新 hook（PreToolUse ExitPlanMode） |
| `pre-agent-gate-check.mjs` | Phase 1.75 Task 1.75.1 | 新 hook（PreToolUse Agent） |
| `ledger-manager.mjs` | Phase 4.75 Task 4.75.1 | 新共用模組 |
| `workflow-gates.mjs` | Phase 4.75 Task 4.75.2 | 新共用模組（從 workflow-state 抽出） |
| `commit-gate-validator.mjs` | Phase 4.75 Task 4.75.3 | 新共用模組（從 pre-bash-check 抽出） |
| `commit-message-enhancer.mjs` | Phase 4.75 Task 4.75.3 | 新共用模組（從 pre-bash-check 抽出） |

### 既有檔案完整替換（8 個）

| 檔案 | 對應 Phase / Task | 主要變更 |
|------|-------------------|---------|
| `workflow-state.mjs` | Phase 0.2 + 1.3 | v2 schema + v1→v2 自動遷移 + 5 gate + overrides |
| `post-agent-verify.mjs` | Phase 1.4 + 4.5.2 | AGENT_STEP_MAP 擴為 9 條 + 自動 Ledger 生成 |
| `pre-bash-check.mjs` | Phase 1.5 + 4.75.3 | 拆出 validator + enhancer，commit gate 用新 helper |
| `pre-edit-check.mjs` | Phase 0.3 | Main branch fail-closed + `HANS_OS_SKIP_BRANCH_CHECK=1` 逃生閥 |
| `workflow-orchestrator.mjs` | Phase 4.25.2 + 4.5.6 | 通用 `workflow override` 指令 + 5-gate 提示 |
| `session-start.mjs` | Phase 5.3 + 0.4 | stop-gate 警示 + 4 reviewer reset + 移除 findings.md 自動建 |
| `on-stop.mjs` | Phase 0.4a | stepNames 改新 schema |
| `pre-compact.mjs` | Phase 0.4b + 0.4c | stepNames 改新 schema + 刪 findings.md dead code + 讀 deferred.md |

### 輔助文件（2 個）

| 檔案 | 用途 |
|------|------|
| `MIGRATION.md` | 原逐 phase 修改指南（現在可直接用完整替換，此文件備查） |
| `settings.local.json.snippet.md` | settings.local.json 要加的 2 條 registration |

---

## 分 Phase 逐步部署（若要謹慎）

```bash
# Phase 0
cp .claude/rules/hook-drafts/workflow-state.mjs .claude/hooks/
cp .claude/rules/hook-drafts/pre-edit-check.mjs .claude/hooks/
cp .claude/rules/hook-drafts/on-stop.mjs .claude/hooks/
cp .claude/rules/hook-drafts/pre-compact.mjs .claude/hooks/
cp .claude/rules/hook-drafts/pre-exit-plan-check.mjs .claude/hooks/
rm .claude/workflow/findings.md
# 手動改 state.json 刪殘欄位（或讓 workflow-state.mjs 的 migration 自動處理）

# Phase 1（Gate A 部分）
cp .claude/rules/hook-drafts/post-agent-verify.mjs .claude/hooks/  # AGENT_STEP_MAP 擴充

# Phase 1.75
cp .claude/rules/hook-drafts/pre-agent-gate-check.mjs .claude/hooks/
# 手動改 settings.local.json（見 snippet.md）

# Phase 4.25
cp .claude/rules/hook-drafts/workflow-orchestrator.mjs .claude/hooks/  # 通用 override 指令

# Phase 4.5
# post-agent-verify.mjs 已含 Ledger 生成
cp .claude/rules/hook-drafts/pre-bash-check.mjs .claude/hooks/  # 新 validator 整合

# Phase 4.75
cp .claude/rules/hook-drafts/ledger-manager.mjs .claude/hooks/
cp .claude/rules/hook-drafts/workflow-gates.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-gate-validator.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-message-enhancer.mjs .claude/hooks/

# Phase 5
cp .claude/rules/hook-drafts/session-start.mjs .claude/hooks/  # stop-gate 警示
/codex:setup --enable-review-gate
```

---

## 部署後最終驗證

```bash
# 1. 語法檢查（全 16 個 hook）
for f in .claude/hooks/*.mjs; do
  node --check "$f" && echo "OK: $(basename $f)"
done

# 2. v1 state migration 測試
node -e "
  import('./.claude/hooks/workflow-state.mjs').then(m => {
    const old = { completedSteps: { codeReview: true, linusReview: true } };
    m.setWorkflowState(old);
    const migrated = m.getWorkflowState();
    console.assert(migrated.schemaVersion === 2, 'schemaVersion should be 2');
    console.assert(migrated.completedSteps.gateSafetyDone === true);
    console.assert(migrated.completedSteps.gateProjectFitDone === true);
    console.assert(migrated.completedSteps.gateCleanupDone === true);
    console.assert(migrated.completedSteps.gateTasteDone === true);
    console.assert(migrated.completedSteps.codeReview === undefined);
    console.log('Migration OK');
  });
"

# 3. 新 schema 初始化
node -e "
  import('./.claude/hooks/workflow-state.mjs').then(m => {
    m.resetWorkflowState();
    const s = m.getWorkflowState();
    console.log('Fresh state:', JSON.stringify(s.completedSteps, null, 2));
  });
"

# 4. Gate completion API
node -e "
  import('./.claude/hooks/workflow-gates.mjs').then(m => {
    console.log('allCodingGatesComplete:', m.allCodingGatesComplete());
    console.log('missing:', m.getCodingMissingGates());
  });
"

# 5. Ledger parsing（手動建一個測試 ledger）
mkdir -p .claude/workflow
cat > .claude/workflow/ledger-gateA-security-20260423-1500.md <<'EOF'
# Ledger: gateA — security
Task: test
Generated: 2026-04-23T15:00:00Z

| # | Severity | File:Line | Title | Disposition | Evidence |
|---|----------|-----------|-------|-------------|----------|
| 1 | critical | Foo.cs:45 | Test critical | [ ] | |
| 2 | low | Bar.cs:10 | Test low | DISMISSED (auto) | low severity |
EOF
node -e "
  import('./.claude/hooks/ledger-manager.mjs').then(m => {
    const l = m.loadLedger('.claude/workflow/ledger-gateA-security-20260423-1500.md');
    const u = m.getUndisposedFindings(l);
    console.log('undisposed:', u.length, '(expected 1)');
  });
"
rm .claude/workflow/ledger-gateA-security-20260423-1500.md
```

如果以上全部通過，新 pipeline 可以開始使用。

## 失敗排查

- `workflow-state.mjs` 遷移後舊 task 卡住 → `workflow reset` 重新開始
- `workflow-gates.mjs` import error → 確認 `ledger-manager.mjs` 已複製
- `pre-bash-check.mjs` 永遠擋 commit → 檢查 `commit-gate-validator.mjs` 是否複製，或 `workflow override <target>` 臨時繞過
- `pre-agent-gate-check.mjs` 誤擋 → `workflow override unblock-next <reason ≥ 20>` 下次 dispatch 放行一次

## 回滾

```bash
# 還原所有 hook（從 backup）
cp -r .claude/hooks.backup-YYYYMMDD-HHMM/* .claude/hooks/

# 還原 gitignore + state.json
git checkout main -- .gitignore
# state.json 視情況：migration 若有 corrupt，直接刪 state.json 讓 default 重生
```
