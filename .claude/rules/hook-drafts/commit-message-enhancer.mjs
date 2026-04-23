// ============================================================================
// commit-message-enhancer.mjs - Commit message augmentation
// ============================================================================
// Extracted from pre-bash-check.mjs to keep main hook lean.
// Adds Ledger-Refs: and Override-{target}-Reason: fields to commit messages.
//
// NOTE: Claude Code's Bash tool currently does not support rewriting the commit
// message at PreToolUse time. The expectation is that main Claude builds the
// message with these fields based on guidance from workflow-orchestrator.mjs,
// OR this module produces a text block to append to the user's draft message.
//
// Callers:
//   - pre-bash-check.mjs (optionally, if message rewriting is implemented)
//   - workflow-orchestrator.mjs (generate the block for `commit this` guidance)
// ============================================================================

import { readdirSync, existsSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { loadLedger, summarizeLedger } from './ledger-manager.mjs';
import { getWorkflowState } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

// ── Build Ledger-Refs: block from all current Code phase ledgers ────────────

export function buildLedgerRefsBlock() {
  if (!existsSync(WORKFLOW_DIR)) return '';

  const gateLabels = {
    gateA: 'Gate A',
    gateB: 'Gate B',
    gateC: 'Gate C',
    gateD: 'Gate D',
    gatex: 'Gate X',
  };

  const lines = [];
  const ledgerFiles = readdirSync(WORKFLOW_DIR)
    .filter((f) => /^ledger-(gateA|gateB|gateC|gateD|gatex)-.*\.md$/i.test(f))
    .sort();

  for (const ledgerFile of ledgerFiles) {
    const match = ledgerFile.match(/^ledger-(gateA|gateB|gateC|gateD|gatex)-/i);
    if (!match) continue;
    const gate = match[1].toLowerCase();
    const label = gateLabels[gate] || gate;

    const ledger = loadLedger(resolve(WORKFLOW_DIR, ledgerFile));
    const stats = summarizeLedger(ledger);

    const parts = [];
    if (stats.fixed) parts.push(`${stats.fixed} fixed`);
    if (stats.incorporated) parts.push(`${stats.incorporated} incorporated`);
    if (stats.deferred) parts.push(`${stats.deferred} deferred`);
    if (stats.dismissed) parts.push(`${stats.dismissed} dismissed`);
    if (stats.pending) parts.push(`${stats.pending} PENDING`);

    lines.push(`- ${label}: ${stats.total} findings / ${parts.join(' / ') || 'none'}`);
  }

  if (lines.length === 0) return '';
  return `Ledger-Refs:\n${lines.join('\n')}\n`;
}

// ── Build Override-*-Reason: block from state.overrides ─────────────────────

export function buildOverrideReasonBlock() {
  const state = getWorkflowState();
  const overrides = state.overrides || {};
  const lines = [];

  for (const [target, data] of Object.entries(overrides)) {
    if (!data) continue;
    if (typeof data === 'object' && data.active === false) continue; // skip expired
    const reason = typeof data === 'object' ? data.reason : String(data);
    if (!reason) continue;
    lines.push(`Override-${target}-Reason: ${reason}`);
  }

  if (lines.length === 0) return '';
  return lines.join('\n') + '\n';
}

// ── Build Plan-Review-Ledger-Refs: for plan file ────────────────────────────

export function buildPlanReviewLedgerRefsBlock() {
  if (!existsSync(WORKFLOW_DIR)) return '';

  const lines = [];
  const ledgerFiles = readdirSync(WORKFLOW_DIR)
    .filter((f) => /^ledger-plan-.*\.md$/i.test(f))
    .sort();

  for (const ledgerFile of ledgerFiles) {
    const match = ledgerFile.match(/^ledger-plan-(\w+)-/i);
    if (!match) continue;
    const reviewer = match[1];

    const ledger = loadLedger(resolve(WORKFLOW_DIR, ledgerFile));
    const stats = summarizeLedger(ledger);

    const parts = [];
    if (stats.incorporated) parts.push(`${stats.incorporated} incorporated`);
    if (stats.deferred) parts.push(`${stats.deferred} deferred`);
    if (stats.dismissed) parts.push(`${stats.dismissed} dismissed`);

    lines.push(`- plan-${reviewer}-reviewer: ${stats.total} findings / ${parts.join(' / ') || 'none'}`);
  }

  if (lines.length === 0) return '';
  return `Plan-Review-Ledger-Refs:\n${lines.join('\n')}\n`;
}

// ── Compose full enhancement block (for commit message appending) ───────────

export function enhanceCommitMessage(originalMessage) {
  const ledgerBlock = buildLedgerRefsBlock();
  const overrideBlock = buildOverrideReasonBlock();

  let enhancement = '';
  if (ledgerBlock) enhancement += '\n' + ledgerBlock;
  if (overrideBlock) enhancement += '\n' + overrideBlock;

  if (!enhancement) return originalMessage;

  // Append before any trailing Co-Authored-By or similar trailers
  return originalMessage.trimEnd() + '\n' + enhancement;
}
