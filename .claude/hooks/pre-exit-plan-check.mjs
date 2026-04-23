// ============================================================================
// pre-exit-plan-check.mjs - PreToolUse(ExitPlanMode) Hook
// ============================================================================
// Blocks ExitPlanMode until all Plan Phase reviewers complete and Plan Ledger
// findings are disposed.
//
// Requirements for passing:
// 1. All 4 plan reviewers completed (ceoReview + engReview + planLinusReview + planCodexReview)
// 2. planCodexVerdict !== "needs-attention" (or explicit override via workflow override planCodex)
// 3. All ledger-plan-*.md MEDIUM+ findings have disposition
//
// Exit codes:
//   0 = allow ExitPlanMode
//   2 = block ExitPlanMode (with stderr message)
// ============================================================================

import { existsSync, readFileSync, readdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { parseHookInput, log } from './hook-utils.mjs';
import { getWorkflowState } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

// ── 1. Check all 4 plan reviewers completed ─────────────────────────────────

let state;
try {
  state = getWorkflowState();
} catch {
  log('[pre-exit-plan-check] WARNING: state read failed, allowing exit');
  process.exit(0);
}

const steps = state.completedSteps || {};
const required = [
  ['ceoReview', 'CEO Review'],
  ['engReview', 'Eng Review'],
  ['planLinusReview', 'Plan Linus Review'],
  ['planCodexReview', 'Plan Codex Adversarial Review'],
];

const missing = required.filter(([key]) => !steps[key]).map(([, name]) => name);

if (missing.length > 0) {
  process.stderr.write(
    `BLOCKED: Cannot ExitPlanMode. Missing plan reviewers:\n` +
    missing.map((m) => `  - ${m}`).join('\n') + '\n\n' +
    `Dispatch the missing reviewer(s) via Agent tool (single message, parallel ok):\n` +
    `  Agent → subagent_type="plan-codex-adversarial-reviewer"\n\n` +
    `Retry ExitPlanMode after all 4 reviewers complete.\n`
  );
  process.exit(2);
}

// ── 2. Check planCodexVerdict ───────────────────────────────────────────────

const verdict = steps.planCodexVerdict;
const planCodexOverride = Boolean(state.overrides && state.overrides.planCodex);

if (verdict === 'needs-attention' && !planCodexOverride) {
  process.stderr.write(
    `BLOCKED: Plan Codex Adversarial Review verdict is "needs-attention".\n` +
    `Address the findings in the most recent ledger-plan-codex-*.md,\n` +
    `then re-dispatch plan-codex-adversarial-reviewer to get a fresh verdict.\n\n` +
    `Emergency override (documented reason ≥ 20 chars):\n` +
    `  workflow override planCodex <reason>\n`
  );
  process.exit(2);
}

// ── 3. Check Plan Ledger dispositions ───────────────────────────────────────

if (existsSync(WORKFLOW_DIR)) {
  try {
    const ledgers = readdirSync(WORKFLOW_DIR)
      .filter((f) => /^ledger-plan-.*\.md$/i.test(f));

    const undisposed = [];
    for (const ledgerFile of ledgers) {
      const fullPath = resolve(WORKFLOW_DIR, ledgerFile);
      const content = readFileSync(fullPath, 'utf-8');
      const rows = content.split('\n').filter((line) => /^\|\s*\d+\s*\|/.test(line));
      for (const row of rows) {
        const cells = row.split('|').map((c) => c.trim());
        if (cells.length < 6) continue;
        const severity = (cells[2] || '').toLowerCase();
        const disposition = (cells[5] || '').toLowerCase();
        const isMediumPlus = ['critical', 'high', 'medium'].includes(severity);
        const isEmpty = disposition === '' || disposition === '[ ]';
        if (isMediumPlus && isEmpty) {
          undisposed.push(`${ledgerFile} — severity=${severity}, row=${cells[1]}`);
        }
      }
    }

    if (undisposed.length > 0) {
      process.stderr.write(
        `BLOCKED: Plan Ledger has ${undisposed.length} undisposed MEDIUM+ findings:\n` +
        undisposed.slice(0, 10).map((u) => `  - ${u}`).join('\n') + '\n' +
        (undisposed.length > 10 ? `  ... and ${undisposed.length - 10} more\n` : '') + '\n' +
        `Each finding must have disposition: INCORPORATED / DISMISSED + reason / DEFERRED + deferred.md#entry-{id}\n`
      );
      process.exit(2);
    }
  } catch (err) {
    log(`[pre-exit-plan-check] WARNING: ledger scan failed: ${err.message}`);
  }
}

log('[pre-exit-plan-check] All 4 reviewers + Codex verdict + Plan Ledger disposition OK');
process.exit(0);
