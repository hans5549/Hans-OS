# Hook Migration Guide

本指南列出 `.claude/hooks/` 內**既有** hook 檔案需要的修改。因為 `pre-edit-check.mjs` 保護這些檔案，Claude 無法直接修改，需要使用者手動執行。

搭配 `.claude/rules/hook-drafts/` 的新 hook 檔 drafts 一起使用。

---

## 修改順序建議

按 Phase 順序（與計畫一致）：

1. **Phase 0**：`workflow-state.mjs` / `state.json` / `pre-edit-check.mjs` / `on-stop.mjs` / `pre-compact.mjs`
2. **Phase 1 Task 1.3-1.5**：`post-agent-verify.mjs` / `pre-bash-check.mjs`
3. **Phase 1.75**：複製 `pre-agent-gate-check.mjs` draft + `settings.local.json` 註冊
4. **Phase 4.25 Task 4.25.2**：`workflow-orchestrator.mjs` 加通用 override 指令
5. **Phase 4.5**：`post-agent-verify.mjs` ledger 生成 / `pre-bash-check.mjs` ledger 檢查
6. **Phase 4.75**：複製 4 個 helper drafts + 改既有 hook import
7. **Phase 5 Task 5.3**：`session-start.mjs` 加 stop-gate 警示

---

## Phase 0 修改

### Phase 0 Task 0.1：pre-exit-plan-check.mjs
```bash
cp .claude/rules/hook-drafts/pre-exit-plan-check.mjs .claude/hooks/
```
然後在 `.claude/settings.local.json` 的 `hooks.PreToolUse` 加入：
```json
{
  "matcher": "ExitPlanMode",
  "hooks": [
    { "type": "command", "command": "node .claude/hooks/pre-exit-plan-check.mjs", "timeout": 10 }
  ]
}
```
**注意**：settings.local.json 可能已有此註冊（hook 已宣告但檔案不存在）。若已有，只需補上檔案。

### Phase 0 Task 0.2：state.json 殘欄位清理

刪除以下 6 個 key（若存在）：
```json
// .claude/workflow/state.json
{
  "simplifier": ...,        ← 刪
  "specCheck": ...,         ← 刪
  "codeReviewer": ...,      ← 刪
  "securityReviewer": ...,  ← 刪
  "linusGreen": ...,        ← 刪
  "mergeBackPending": ...   ← 刪
}
```

同時更新 `.claude/hooks/workflow-state.mjs:26-46` 的 `getDefaultState()` 確認沒這些 key，並加入新 schema（見下一段 Task 1.3）。

### Phase 0 Task 0.3：pre-edit-check.mjs fail-closed

找到 `pre-edit-check.mjs:36-51` 的 `execSync('git branch --show-current')` 區段。當前 catch 只 log warning：
```js
try {
  const branch = execSync('git branch --show-current', ...).trim();
  if ((branch === 'main' || branch === 'master') && !isWorkflowFile) {
    process.exit(2);
  }
} catch (err) {
  log(`[WARNING] Git branch check failed: ${err.message}`);  // ← 目前只警告
}
```

改為：
```js
try {
  const branch = execSync('git branch --show-current', ...).trim();
  if ((branch === 'main' || branch === 'master') && !isWorkflowFile) {
    process.exit(2);
  }
} catch (err) {
  // Fail-closed: block edits when branch check fails (e.g., detached HEAD)
  // unless explicit opt-out via env var
  if (process.env.HANS_OS_SKIP_BRANCH_CHECK !== '1') {
    process.stderr.write(
      `BLOCKED: Unable to verify branch (${err.message}).\n` +
      `Set HANS_OS_SKIP_BRANCH_CHECK=1 to bypass (only for detached HEAD or special cases).\n`
    );
    process.exit(2);
  }
  log(`[WARNING] Git branch check failed but bypass env var set: ${err.message}`);
}
```

### Phase 0 Task 0.4a：on-stop.mjs 舊 step 名

找到 `on-stop.mjs:41-48`：
```js
const stepNames = [
  ['ceoReview', 'CEO Review'],
  ['engReview', 'Eng Review'],
  ['planLinusReview', 'Plan Linus'],
  ['codeReview', 'Code Review'],           // ← 刪
  ['linusReview', 'Linus Review'],          // ← 改名
  ['buildPassed', 'Build'],
];
```

改為：
```js
const stepNames = [
  // Planning phase
  ['ceoReview', 'CEO Review'],
  ['engReview', 'Eng Review'],
  ['planLinusReview', 'Plan Linus'],
  ['planCodexReview', 'Plan Codex'],
  // Coding phase
  ['gateSafetyDone', 'Gate A Safety'],
  ['gateProjectFitDone', 'Gate B Project Fit'],
  ['gateTasteDone', 'Gate C Taste'],
  ['gateCleanupDone', 'Gate D Cleanup'],
  ['gateXCodexDone', 'Gate X Codex'],
  ['buildPassed', 'Build'],
];
```

### Phase 0 Task 0.4b：pre-compact.mjs 舊 step 名

找到 `pre-compact.mjs:69-73`：
```js
const stepNames = [
  ['codeReview', 'Review'],    // ← 刪
  ['linusReview', 'Linus'],    // ← 刪
  ['buildPassed', 'Build'],
];
```

改為（同上 on-stop 的完整清單）。

### Phase 0 Task 0.4c：pre-compact.mjs findings.md dead code

找到 `pre-compact.mjs:120-128`：
```js
const findingsPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'findings.md');
if (existsSync(findingsPath)) {
  const findings = readFileSync(findingsPath, 'utf-8');
  if (findings.trim().split('\n').length > 3) {
    log('[Findings Summary]');
    log(readFirstNLines(findingsPath, 20));
    log('');
  }
}
```

**整段刪除**，改為讀 `deferred.md`：
```js
const deferredPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'deferred.md');
if (existsSync(deferredPath)) {
  const deferred = readFileSync(deferredPath, 'utf-8');
  const openEntries = (deferred.match(/^## entry-\d+[\s\S]*?Status:\s*open/gm) || []).length;
  if (openEntries > 0) {
    log(`[Context] ${openEntries} deferred findings awaiting future handling`);
    log(readFirstNLines(deferredPath, 20));
    log('');
  }
}
```

然後刪除 `.claude/workflow/findings.md`（是 dead file）。

---

## Phase 1 Task 1.3-1.5 修改

### Task 1.3：workflow-state.mjs schema

找到 `getDefaultState()` 函數（約 line 26-46），**完整替換**為：

```js
function getDefaultState() {
  return {
    schemaVersion: 2,
    modifiedFiles: [],
    completedSteps: {
      // Planning phase
      planner: false,
      ceoReview: false,
      engReview: false,
      planLinusReview: false,
      planCodexReview: false,
      planCodexVerdict: null,      // "approve" | "needs-attention" | "degraded" | null
      // Coding phase
      gateSafetyDone: false,
      gateProjectFitDone: false,
      gateTasteDone: false,
      gateCleanupDone: false,
      gateXCodexDone: false,
      gateXCodexVerdict: null,     // "approve" | "needs-attention" | null
      buildPassed: false,
    },
    overrides: {},                  // { gateX: { reason, timestamp }, unblock-next: { active: true, reason }, ... }
    buildRetryCount: 0,
    lastModified: '',
    sessionId: '',
    currentPlanFile: '',
    lineChangeSinceReview: 0,
  };
}
```

在 `getWorkflowState()` 函數中加入**遷移邏輯**（在 read state 後、return 前）：

```js
function getWorkflowState() {
  // ... existing read logic ...

  // v1 → v2 migration
  if (!state.schemaVersion || state.schemaVersion < 2) {
    if (state.completedSteps?.codeReview === true) {
      state.completedSteps.gateSafetyDone = true;
      state.completedSteps.gateProjectFitDone = true;
      state.completedSteps.gateCleanupDone = true;
      delete state.completedSteps.codeReview;
    }
    if (state.completedSteps?.linusReview === true) {
      state.completedSteps.gateTasteDone = true;
      delete state.completedSteps.linusReview;
    }
    if (state.completedSteps) {
      state.completedSteps.gateXCodexDone ??= false;
      state.completedSteps.gateXCodexVerdict ??= null;
      state.completedSteps.planCodexReview ??= false;
      state.completedSteps.planCodexVerdict ??= null;
    }
    state.overrides ??= {};
    state.schemaVersion = 2;
    setWorkflowState(state);
  }

  return state;
}
```

### Task 1.4：post-agent-verify.mjs AGENT_STEP_MAP

找到 `AGENT_STEP_MAP`（約 line 23-34）。**完整替換**為：

```js
const AGENT_STEP_MAP = [
  // Plan phase（4 reviewers, all Agent tool）
  { pattern: /plan-ceo-review/i, step: 'ceoReview', name: 'CEO Review' },
  { pattern: /plan-eng-review/i, step: 'engReview', name: 'Eng Review' },
  { pattern: /plan-linus-review/i, step: 'planLinusReview', name: 'Plan Linus Review' },
  { pattern: /plan-codex-adversarial-reviewer/i, step: 'planCodexReview', name: 'Plan Codex Adversarial Review' },
  // Code phase Gate A-D
  { pattern: /security-vuln-scanner/i, step: 'gateSafetyDone', name: 'Gate A Safety' },
  { pattern: /code-review-specialist/i, step: 'gateProjectFitDone', name: 'Gate B Project Fit' },
  { pattern: /linus-reviewer/i, step: 'gateTasteDone', name: 'Gate C Taste' },
  { pattern: /code-simplifier/i, step: 'gateCleanupDone', name: 'Gate D Cleanup' },
  // Code phase Gate X (Codex wrapper)
  { pattern: /gatex-codex-reviewer/i, step: 'gateXCodexDone', name: 'Gate X Cross-Model' },
];
```

### Task 1.4 (extension)：post-agent-verify.mjs ledger 生成

在 `AGENT_STEP_MAP` 迴圈**後面**加入 ledger 生成邏輯（Phase 4.5.2 一併處理）：

```js
// After marking step complete, build ledger from agent output
import { parseStructuredFindings, buildLedger, writeLedger, buildLedgerPath, timestampNow } from './ledger-manager.mjs';

// tool_response 裡會有 agent 的 markdown 輸出
const agentOutput = parsed.tool_response?.content || parsed.tool_response?.text || '';
const findings = parseStructuredFindings(agentOutput);

if (findings.length > 0 || /Machine-Readable Findings/i.test(agentOutput)) {
  const phase = /^plan-/i.test(subagentType) ? 'plan' : 'code';
  let gate, shortName;

  if (phase === 'plan') {
    gate = 'plan';
    if (/ceo/i.test(subagentType)) shortName = 'ceo';
    else if (/eng/i.test(subagentType)) shortName = 'eng';
    else if (/linus/i.test(subagentType)) shortName = 'linus';
    else if (/codex/i.test(subagentType)) shortName = 'codex';
    else shortName = 'unknown';
  } else {
    // Code phase
    if (/security/i.test(subagentType))     { gate = 'gateA'; shortName = 'security'; }
    else if (/specialist/i.test(subagentType)) { gate = 'gateB'; shortName = 'specialist'; }
    else if (/linus/i.test(subagentType))    { gate = 'gateC'; shortName = 'linus'; }
    else if (/simplifier/i.test(subagentType)) { gate = 'gateD'; shortName = 'simplifier'; }
    else if (/gatex/i.test(subagentType))    { gate = 'gatex'; shortName = 'codex'; }
    else { gate = 'unknown'; shortName = 'unknown'; }
  }

  const ts = timestampNow();
  const path = buildLedgerPath(phase, gate, shortName, ts);
  const state = getWorkflowState();
  const ledgerContent = buildLedger({
    gate,
    agent: shortName,
    task: state.currentTask || '(unknown)',
    findings,
  });
  writeLedger(path, ledgerContent);
  log(`[Workflow] Ledger created: ${path}`);
}
```

### Task 1.5：pre-bash-check.mjs commit gate

找到 `getCodingMissingSteps` 或類似的 commit gate 檢查邏輯。**完整替換**使用新 helper：

```js
import { validateCommitPreconditions } from './commit-gate-validator.mjs';
import { buildLedgerRefsBlock, buildOverrideReasonBlock } from './commit-message-enhancer.mjs';

// In the git commit detection block:
if (isCommit) {
  const result = validateCommitPreconditions();
  if (!result.ok) {
    process.stderr.write(result.blocker);
    process.exit(2);
  }
  if (result.warnings?.length) {
    result.warnings.forEach((w) => log(`[Workflow WARNING] ${w}`));
  }
  // Optionally prepend Ledger-Refs guidance to stderr if commit message lacks them
}
```

---

## Phase 1.75 Task 1.75.3 + 4.75 新 hook 註冊

### settings.local.json 新增 hook registration

```json
{
  "hooks": {
    "PreToolUse": [
      // existing entries...
      {
        "matcher": "Agent",
        "hooks": [
          { "type": "command", "command": "node .claude/hooks/pre-agent-gate-check.mjs", "timeout": 10 }
        ]
      },
      {
        "matcher": "ExitPlanMode",
        "hooks": [
          { "type": "command", "command": "node .claude/hooks/pre-exit-plan-check.mjs", "timeout": 10 }
        ]
      }
    ]
  }
}
```

### 複製 hook drafts

```bash
cp .claude/rules/hook-drafts/pre-exit-plan-check.mjs .claude/hooks/
cp .claude/rules/hook-drafts/pre-agent-gate-check.mjs .claude/hooks/
cp .claude/rules/hook-drafts/ledger-manager.mjs .claude/hooks/
cp .claude/rules/hook-drafts/workflow-gates.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-gate-validator.mjs .claude/hooks/
cp .claude/rules/hook-drafts/commit-message-enhancer.mjs .claude/hooks/
```

---

## Phase 4.25 Task 4.25.2：workflow-orchestrator.mjs 通用 override 指令

在 workflow command 偵測區段（應該有 `workflow status` / `workflow reset` / `workflow skip` 等判斷）加入：

```js
// Match: workflow override <target> <reason>
const overrideMatch = promptLower.match(/workflow\s+override\s+(\w+)\s+(.+)/i);
if (overrideMatch) {
  const [, target, reasonRaw] = overrideMatch;
  const reason = reasonRaw.trim();
  const validTargets = ['gateA', 'gateB', 'gateC', 'gateD', 'gateX', 'planCodex', 'unblock-next'];

  if (!validTargets.includes(target)) {
    log(`[Workflow] ERROR: Invalid override target "${target}". Valid: ${validTargets.join(', ')}`);
    process.exit(0);
  }
  if (reason.length < 20) {
    log(`[Workflow] ERROR: Override reason must be ≥ 20 characters (got ${reason.length}).`);
    process.exit(0);
  }

  const state = getWorkflowState();
  state.overrides = state.overrides || {};
  state.overrides[target] = { reason, timestamp: new Date().toISOString() };
  if (target === 'unblock-next') state.overrides[target].active = true;
  setWorkflowState(state);

  // Append to skip-log.md
  const logPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'skip-log.md');
  const logEntry = `\n## ${new Date().toISOString()} — override ${target}\nReason: ${reason}\n`;
  try { appendFileSync(logPath, logEntry); } catch { /* non-critical */ }

  log(`[Workflow] Override registered: ${target}`);
  log(`  Commit message will include: Override-${target}-Reason: ${reason.slice(0, 60)}${reason.length > 60 ? '...' : ''}`);
  process.exit(0);
}
```

同時更新 `code-review` / `commit this` 的 dispatch pattern 提示文字為新 5-gate 順序。

---

## Phase 5 Task 5.3：session-start.mjs stop-gate 警示

在 session start banner 輸出結尾加入：

```js
// Check Codex stop-review-gate status
const codexConfigPath = resolve(process.env.HOME, '.codex', 'config.json');
if (existsSync(codexConfigPath)) {
  try {
    const codexConfig = JSON.parse(readFileSync(codexConfigPath, 'utf-8'));
    if (codexConfig.stopReviewGate === true) {
      log('');
      log('⚠️  Codex stop-review-gate is ON');
      log('   Stop will be blocked if Codex finds issues in your last-turn edits.');
      log('   Monitor OpenAI quota. Emergency disable: /codex:setup --disable-review-gate');
      log('');
    }
  } catch { /* non-critical */ }
}
```

---

## 完成驗證

所有 hook 改完後執行：

```bash
# 1. 語法檢查
for f in hook-utils workflow-state workflow-gates ledger-manager \
         workflow-orchestrator pre-edit-check pre-bash-check \
         commit-gate-validator commit-message-enhancer \
         pre-exit-plan-check pre-agent-gate-check \
         post-edit-build post-agent-verify \
         session-start on-stop pre-compact; do
  node --check ".claude/hooks/${f}.mjs" && echo "OK: $f" || echo "FAIL: $f"
done

# 2. Schema 遷移測試
cd Hans-OS && node -e "
  import('./.claude/hooks/workflow-state.mjs').then(m => {
    m.setWorkflowState({ completedSteps: { codeReview: true, linusReview: true } });
    const s = m.getWorkflowState();
    console.log('schemaVersion:', s.schemaVersion);
    console.log('gateSafetyDone:', s.completedSteps.gateSafetyDone);
    console.log('gateProjectFitDone:', s.completedSteps.gateProjectFitDone);
    console.log('gateTasteDone:', s.completedSteps.gateTasteDone);
    console.log('gateCleanupDone:', s.completedSteps.gateCleanupDone);
    console.log('gateXCodexDone:', s.completedSteps.gateXCodexDone);
  });
"

# 3. 清理 drafts
rm -rf .claude/rules/hook-drafts/

# 4. 執行 Phase 5 Task 5.1
/codex:setup --enable-review-gate

# 5. 建 feature branch 跑一個小 task 做端到端驗證
```

---

## 如果卡住

- Hook 改動導致 commit 永遠被擋 → 暫時 `git commit --no-verify` 再慢慢查（只在緊急時用）
- Schema 遷移後舊 task 卡住 → `workflow reset` 重置
- Codex wrapper 跑不動 → `workflow override gateX <20+ char reason>` + `workflow override planCodex <20+ char reason>` 暫時繞過
- 全面崩潰 → `git checkout main -- .claude/hooks/` 還原 hook，重新規劃

祝順利。
