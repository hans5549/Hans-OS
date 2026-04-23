// ============================================================================
// ledger-manager.mjs - Shared Findings Ledger utilities
// ============================================================================
// Used by:
//   - post-agent-verify.mjs   (build ledger after agent completion)
//   - pre-agent-gate-check.mjs (check previous gate's ledger disposition)
//   - pre-bash-check.mjs       (check all ledger disposition before commit)
//   - pre-exit-plan-check.mjs  (check plan ledger disposition)
// ============================================================================

import { existsSync, readFileSync, writeFileSync, readdirSync, mkdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

// ── Ledger path builder ─────────────────────────────────────────────────────

export function buildLedgerPath(phase, gate, agent, timestamp) {
  // phase: 'plan' | 'code'
  // gate:  'plan' | 'gateA' | 'gateB' | 'gateC' | 'gateD' | 'gatex'
  // agent: short name (ceo | eng | linus | codex | security | specialist | simplifier)
  // timestamp: YYYYMMDD-HHmm
  const filename = `ledger-${gate}-${agent}-${timestamp}.md`;
  return resolve(WORKFLOW_DIR, filename);
}

export function timestampNow() {
  const d = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}-${pad(d.getHours())}${pad(d.getMinutes())}`;
}

// ── Structured Findings parser ──────────────────────────────────────────────

// Extracts rows from a "Machine-Readable Findings" table embedded in agent output
export function parseStructuredFindings(markdown) {
  if (!markdown) return [];
  const lines = markdown.split('\n');
  const findings = [];

  // Find the "## Machine-Readable Findings" section start
  let inTable = false;
  let headerSeen = false;
  for (const line of lines) {
    if (/^##\s+Machine-Readable\s+Findings/i.test(line)) {
      inTable = true;
      headerSeen = false;
      continue;
    }
    if (!inTable) continue;
    if (/^##\s+/.test(line)) break; // next section ends the table

    // Skip table header and separator
    if (/^\|\s*#\s*\|/.test(line)) { headerSeen = true; continue; }
    if (/^\|\s*-+\s*\|/.test(line)) continue;
    if (!headerSeen) continue;

    // Parse data row
    const cells = line.split('|').map((c) => c.trim());
    if (cells.length < 7) continue;
    const [, idx, severity, file, lineNum, title, recommendation] = cells;
    if (!idx || !severity) continue;

    findings.push({
      idx: parseInt(idx, 10),
      severity: severity.toLowerCase(),
      file,
      line: lineNum,
      title,
      recommendation: recommendation || '',
    });
  }

  return findings;
}

// ── Ledger file builder ─────────────────────────────────────────────────────

export function buildLedger({ gate, agent, task, findings }) {
  const autoDismissable = new Set(['low', 'info']);
  const tsIso = new Date().toISOString();

  let body = `# Ledger: ${gate} — ${agent}\n`;
  body += `Task: ${task || '(unknown)'}\n`;
  body += `Generated: ${tsIso}\n\n`;
  body += `| # | Severity | File:Line | Title | Disposition | Evidence |\n`;
  body += `|---|----------|-----------|-------|-------------|----------|\n`;

  for (const f of findings) {
    const autoDismissed = autoDismissable.has(f.severity);
    const disposition = autoDismissed ? 'DISMISSED (auto)' : '[ ]';
    const evidence = autoDismissed ? 'low severity, auto-dismissed per workflow policy' : '';
    const fileLine = f.file + (f.line ? `:${f.line}` : '');
    body += `| ${f.idx} | ${f.severity} | ${fileLine} | ${f.title} | ${disposition} | ${evidence} |\n`;
  }

  body += `\n## Instructions for main Claude\n\n`;
  body += `For each finding with \`[ ]\`, set Disposition to one of:\n`;
  body += `- \`FIXED\` + commit hash or "in-progress file path" (Code phase only)\n`;
  body += `- \`INCORPORATED\` + plan section reference (Plan phase only)\n`;
  body += `- \`DISMISSED\` + free-text reason (false positive / not applicable / trivial)\n`;
  body += `- \`DEFERRED\` + link to \`.claude/workflow/deferred.md#entry-{id}\`\n`;

  return body;
}

export function writeLedger(path, content) {
  const dir = dirname(path);
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  writeFileSync(path, content, 'utf-8');
}

// ── Disposition validator ───────────────────────────────────────────────────

// Parse an existing ledger file and return findings with their disposition state
export function loadLedger(path) {
  if (!existsSync(path)) return { findings: [], exists: false };
  const content = readFileSync(path, 'utf-8');
  const lines = content.split('\n');

  const findings = [];
  for (const line of lines) {
    if (!/^\|\s*\d+\s*\|/.test(line)) continue;
    const cells = line.split('|').map((c) => c.trim());
    if (cells.length < 7) continue;
    const [, idx, severity, fileLine, title, disposition, evidence] = cells;
    findings.push({
      idx: parseInt(idx, 10),
      severity: (severity || '').toLowerCase(),
      fileLine,
      title,
      disposition: (disposition || '').toUpperCase().replace(/\s*\([^)]+\)/, '').trim(),
      rawDisposition: disposition,
      evidence: evidence || '',
    });
  }

  return { findings, exists: true, path };
}

export function getUndisposedFindings(ledger, minSeverity = 'medium') {
  const order = ['info', 'low', 'medium', 'high', 'critical'];
  const minIdx = order.indexOf(minSeverity);
  return ledger.findings.filter((f) => {
    const sev = order.indexOf(f.severity);
    if (sev < minIdx) return false;
    // Disposition missing or empty checkbox
    return !f.disposition || f.disposition === '[ ]' || f.disposition === '';
  });
}

export function validateDisposition(finding) {
  const { disposition, evidence } = finding;
  if (!disposition || disposition === '[ ]') return { ok: false, reason: 'No disposition set' };
  const d = disposition.toUpperCase();
  if (d === 'DISMISSED' && !evidence) return { ok: false, reason: 'DISMISSED requires reason' };
  if (d === 'DEFERRED' && !/deferred\.md#entry-\d+/i.test(evidence)) return { ok: false, reason: 'DEFERRED requires deferred.md#entry-{id} link' };
  if ((d === 'FIXED' || d === 'INCORPORATED') && !evidence) return { ok: true, warning: 'evidence missing (commit hash / file ref / plan section)' };
  return { ok: true };
}

// ── Summarization for commit message ────────────────────────────────────────

export function summarizeLedger(ledger) {
  const stats = { total: 0, fixed: 0, incorporated: 0, dismissed: 0, deferred: 0, pending: 0 };
  for (const f of ledger.findings) {
    stats.total++;
    const d = (f.disposition || '').toUpperCase();
    if (d === 'FIXED') stats.fixed++;
    else if (d === 'INCORPORATED') stats.incorporated++;
    else if (d === 'DISMISSED') stats.dismissed++;
    else if (d === 'DEFERRED') stats.deferred++;
    else stats.pending++;
  }
  return stats;
}

// ── Scan all ledgers for a phase ────────────────────────────────────────────

export function scanLedgers(pattern) {
  if (!existsSync(WORKFLOW_DIR)) return [];
  return readdirSync(WORKFLOW_DIR)
    .filter((f) => pattern.test(f))
    .map((f) => resolve(WORKFLOW_DIR, f));
}
