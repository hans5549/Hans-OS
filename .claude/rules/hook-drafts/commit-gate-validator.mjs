// ============================================================================
// commit-gate-validator.mjs - Commit preconditions validator
// ============================================================================
// Extracted from pre-bash-check.mjs to keep main hook lean.
// Called by pre-bash-check when detecting `git commit` command.
//
// Returns: { ok: boolean, blocker?: string, warnings?: string[] }
// ============================================================================

import { readdirSync, existsSync, readFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import {
  allCodingGatesComplete,
  getCodingMissingGates,
} from './workflow-gates.mjs';
import { loadLedger, getUndisposedFindings, validateDisposition } from './ledger-manager.mjs';
import { getWorkflowState } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

export function validateCommitPreconditions() {
  const warnings = [];

  // ── 1. All 5 coding gates complete ─────────────────────────────────────────
  if (!allCodingGatesComplete()) {
    const missing = getCodingMissingGates();
    return {
      ok: false,
      blocker:
        `Missing coding gates:\n` +
        missing.map((m) => `  - ${m}`).join('\n') + '\n\n' +
        `Dispatch the missing gate agent(s) in sequence.\n` +
        `Each gate requires ledger disposition before the next can dispatch.\n`,
    };
  }

  // ── 2. Gate X Codex verdict must be approve or overridden ──────────────────
  const state = getWorkflowState();
  const verdict = state.completedSteps?.gateXCodexVerdict;
  const gateXOverridden = Boolean(state.overrides?.gateX);
  if (verdict === 'needs-attention' && !gateXOverridden) {
    return {
      ok: false,
      blocker:
        `Gate X Codex verdict is "needs-attention". Address findings in ledger-gatex-codex-*.md,\n` +
        `then re-dispatch gatex-codex-reviewer to get a fresh verdict.\n\n` +
        `Emergency override: workflow override gateX <reason ≥ 20 chars>\n`,
    };
  }

  // ── 3. All Code phase ledger MEDIUM+ findings have disposition ─────────────
  if (existsSync(WORKFLOW_DIR)) {
    const ledgerFiles = readdirSync(WORKFLOW_DIR)
      .filter((f) => /^ledger-(gateA|gateB|gateC|gateD|gatex)-.*\.md$/i.test(f));

    const allUndisposed = [];
    for (const ledgerFile of ledgerFiles) {
      const ledger = loadLedger(resolve(WORKFLOW_DIR, ledgerFile));
      const undisposed = getUndisposedFindings(ledger, 'medium');
      for (const f of undisposed) {
        allUndisposed.push(`${ledgerFile}#row-${f.idx} (severity=${f.severity}): ${f.title}`);
      }
      // Also validate dispositions that exist
      for (const f of ledger.findings) {
        if (!f.disposition || f.disposition === '[ ]') continue;
        const v = validateDisposition(f);
        if (!v.ok) {
          allUndisposed.push(`${ledgerFile}#row-${f.idx}: ${v.reason}`);
        } else if (v.warning) {
          warnings.push(`${ledgerFile}#row-${f.idx}: ${v.warning}`);
        }
      }
    }

    if (allUndisposed.length > 0) {
      return {
        ok: false,
        blocker:
          `Code phase ledger has ${allUndisposed.length} undisposed / invalid MEDIUM+ finding(s):\n` +
          allUndisposed.slice(0, 15).map((u) => `  - ${u}`).join('\n') + '\n' +
          (allUndisposed.length > 15 ? `  ... and ${allUndisposed.length - 15} more\n` : '') + '\n' +
          `Each finding must have valid disposition:\n` +
          `  FIXED + commit hash or file reference\n` +
          `  DISMISSED + non-empty reason\n` +
          `  DEFERRED + deferred.md#entry-{id} link\n`,
      };
    }
  }

  // ── 4. Build passed ────────────────────────────────────────────────────────
  if (!state.completedSteps?.buildPassed) {
    return {
      ok: false,
      blocker: `buildPassed is false. Fix build / typecheck errors and retry commit.\n`,
    };
  }

  return { ok: true, warnings };
}
