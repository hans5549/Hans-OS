// ============================================================================
// pre-compact.mjs - PreCompact Hook: Context Preservation
// ============================================================================
// Injects critical context (branch, workflow state, active plan) into stderr
// BEFORE context compaction, so Claude retains key information after compression.
// ============================================================================

import { execSync } from 'child_process';
import { existsSync, readFileSync, readdirSync, statSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { log } from './hook-utils.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

// ── Gather context ─────────────────────────────────────────────────────────

let branch = '(unknown)';
let recentCommits = '(none)';

try {
  branch = execSync('git branch --show-current', {
    encoding: 'utf-8',
    stdio: ['pipe', 'pipe', 'pipe'],
    cwd: PROJECT_ROOT,
  }).trim();
} catch { /* git not available */ }

try {
  recentCommits = execSync('git log --oneline -3', {
    encoding: 'utf-8',
    stdio: ['pipe', 'pipe', 'pipe'],
    cwd: PROJECT_ROOT,
  }).trim();
} catch { /* git not available */ }

// ── Workflow state ─────────────────────────────────────────────────────────

const STATE_FILE = resolve(PROJECT_ROOT, '.claude', 'workflow', 'state.json');
let workflowInfo = 'No active workflow';

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));
    const files = state.modifiedFiles || [];
    const steps = state.completedSteps || {};

    if (files.length > 0) {
      const stepNames = [
        ['simplifier', 'Simplifier'],
        ['specCheck', 'Spec'],
        ['codeReviewer', 'Review'],
        ['securityReviewer', 'Security'],
        ['linusGreen', 'Linus'],
        ['buildPassed', 'Build'],
      ];

      const status = stepNames
        .map(([k, v]) => `${v} ${steps[k] ? '✓' : '✗'}`)
        .join(', ');

      workflowInfo = `${files.length} tracked files | ${status}`;
    }
  } catch { /* state file corrupt */ }
}

// ── Active plan ────────────────────────────────────────────────────────────

let activePlan = 'None';
const plansDir = resolve(PROJECT_ROOT, '.claude', 'plans');

if (existsSync(plansDir)) {
  try {
    const planFiles = readdirSync(plansDir)
      .filter((f) => f.endsWith('.md'))
      .map((f) => ({
        name: f,
        mtime: statSync(resolve(plansDir, f)).mtimeMs,
      }))
      .sort((a, b) => b.mtime - a.mtime);

    if (planFiles.length > 0) {
      activePlan = `.claude/plans/${planFiles[0].name}`;
    }
  } catch { /* ignore */ }
}

// ── Output ─────────────────────────────────────────────────────────────────

log('');
log('[Context Reminder — PreCompact]');
log(`Branch: ${branch} | Recent commits:`);
for (const line of recentCommits.split('\n')) {
  log(`  ${line}`);
}
log(`Workflow: ${workflowInfo}`);
log(`Active Plan: ${activePlan}`);
log('');

process.exit(0);
