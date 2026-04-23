// ============================================================================
// pre-agent-gate-check.mjs - PreToolUse(Agent) Hook
// ============================================================================
// Enforces sequential Code phase gate dispatch with mandatory ledger disposition
// between gates. Plan phase reviewers dispatch in parallel (exempted).
//
// Code phase gate order: A (security) → B (specialist) → C (linus) → D (simplifier) → X (gatex-codex-reviewer)
// Each gate requires the previous gate's ledger MEDIUM+ findings to be disposed.
//
// Exit codes:
//   0 = allow Agent dispatch
//   2 = block dispatch (with stderr message)
// ============================================================================

import { existsSync, readFileSync, readdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { parseHookInput, log } from './hook-utils.mjs';
import { getWorkflowState, setWorkflowState } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const subagentType = (parsed.tool_input && parsed.tool_input.subagent_type) || '';
if (!subagentType) process.exit(0);

// ── Plan phase reviewers dispatch in parallel — exempted ────────────────────

if (/^plan-/i.test(subagentType)) {
  process.exit(0);
}

// ── unblock-next override — single-use bypass ───────────────────────────────

let state;
try {
  state = getWorkflowState();
} catch {
  process.exit(0);
}

if (state.overrides && state.overrides['unblock-next'] && state.overrides['unblock-next'].active) {
  log('[pre-agent-gate-check] unblock-next override active, allowing dispatch (clearing flag)');
  state.overrides['unblock-next'].active = false;
  try { setWorkflowState(state); } catch { /* non-critical */ }
  process.exit(0);
}

// ── Code phase gate order check ─────────────────────────────────────────────

// Maps the agent being dispatched to the gate it represents, plus prerequisite gates
const GATE_ORDER = [
  { agent: /security-vuln-scanner/i,       gate: 'A', step: 'gateSafetyDone',       ledgerPrefix: null,         prereq: [] },
  { agent: /code-review-specialist/i,      gate: 'B', step: 'gateProjectFitDone',   ledgerPrefix: 'ledger-gateA-', prereq: ['gateSafetyDone'] },
  { agent: /linus-reviewer/i,              gate: 'C', step: 'gateTasteDone',        ledgerPrefix: 'ledger-gateB-', prereq: ['gateSafetyDone', 'gateProjectFitDone'] },
  { agent: /code-simplifier/i,             gate: 'D', step: 'gateCleanupDone',      ledgerPrefix: 'ledger-gateC-', prereq: ['gateSafetyDone', 'gateProjectFitDone', 'gateTasteDone'] },
  { agent: /gatex-codex-reviewer/i,        gate: 'X', step: 'gateXCodexDone',       ledgerPrefix: 'ledger-gateD-', prereq: ['gateSafetyDone', 'gateProjectFitDone', 'gateTasteDone', 'gateCleanupDone'] },
];

const match = GATE_ORDER.find((g) => g.agent.test(subagentType));
if (!match) {
  // Unknown agent — allow (e.g., general-purpose, Explore, etc.)
  process.exit(0);
}

const steps = state.completedSteps || {};

// Check prerequisite gates completed
const missingPrereqs = match.prereq.filter((p) => !steps[p]);
if (missingPrereqs.length > 0) {
  const gateNames = { gateSafetyDone: 'Gate A (Safety)', gateProjectFitDone: 'Gate B (Project Fit)', gateTasteDone: 'Gate C (Taste)', gateCleanupDone: 'Gate D (Cleanup)' };
  process.stderr.write(
    `BLOCKED: Cannot dispatch Gate ${match.gate} (${subagentType}).\n` +
    `Prerequisite gate(s) not yet complete:\n` +
    missingPrereqs.map((p) => `  - ${gateNames[p] || p}`).join('\n') + '\n\n' +
    `Dispatch earlier gates first, or use emergency override:\n` +
    `  workflow override unblock-next <reason ≥ 20 chars>\n`
  );
  process.exit(2);
}

// Check previous gate's ledger disposition
if (match.ledgerPrefix && existsSync(WORKFLOW_DIR)) {
  try {
    const ledgers = readdirSync(WORKFLOW_DIR)
      .filter((f) => f.startsWith(match.ledgerPrefix) && f.endsWith('.md'));

    const undisposed = [];
    for (const ledgerFile of ledgers) {
      const content = readFileSync(resolve(WORKFLOW_DIR, ledgerFile), 'utf-8');
      const rows = content.split('\n').filter((line) => /^\|\s*\d+\s*\|/.test(line));
      for (const row of rows) {
        const cells = row.split('|').map((c) => c.trim());
        if (cells.length < 6) continue;
        const severity = (cells[2] || '').toLowerCase();
        const disposition = (cells[5] || '').toLowerCase();
        if (['critical', 'high', 'medium'].includes(severity) && (disposition === '' || disposition === '[ ]')) {
          undisposed.push(`${ledgerFile}#row-${cells[1]} (severity=${severity})`);
        }
      }
    }

    if (undisposed.length > 0) {
      process.stderr.write(
        `BLOCKED: Cannot dispatch Gate ${match.gate} (${subagentType}).\n` +
        `Previous gate has ${undisposed.length} undisposed MEDIUM+ finding(s):\n` +
        undisposed.slice(0, 10).map((u) => `  - ${u}`).join('\n') + '\n' +
        (undisposed.length > 10 ? `  ... and ${undisposed.length - 10} more\n` : '') + '\n' +
        `Set disposition (FIXED + evidence / DISMISSED + reason / DEFERRED + deferred.md link) in the ledger(s), then retry.\n\n` +
        `Emergency override: workflow override unblock-next <reason ≥ 20 chars>\n`
      );
      process.exit(2);
    }
  } catch (err) {
    log(`[pre-agent-gate-check] WARNING: ledger scan failed: ${err.message}`);
  }
}

log(`[pre-agent-gate-check] Gate ${match.gate} prerequisites satisfied, allowing dispatch`);
process.exit(0);
