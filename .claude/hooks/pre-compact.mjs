// ============================================================================
// pre-compact.mjs - PreCompact Hook: Context Preservation + Reboot Test
// ============================================================================
// Merged from project pre-compact + global pre-compact-context.
// Injects: git context, workflow state, reboot test, plan/findings/progress
// BEFORE context compaction, so Claude retains key information after compression.
// ============================================================================

import { execSync } from 'child_process';
import { existsSync, readFileSync, readdirSync, statSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { log, readFirstNLines } from './hook-utils.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

// ── 1. Reboot Test (from global pre-compact-context) ─────────────────────

log('');
log('[Reboot Test — PreCompact]');
log('Before compaction, confirm you can answer:');
log('  1. Which file/feature am I working on?');
log('  2. What is the end goal of the task?');
log('  3. Which phase of the plan am I in?');
log('  4. What have I learned that affects next steps?');
log('  5. What is done and what remains?');
log('');

// ── 2. Git context ────────────────────────────────────────────────────────

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

log(`[Context] Branch: ${branch} | Recent commits:`);
for (const line of recentCommits.split('\n')) {
  log(`  ${line}`);
}

// ── 3. Workflow state ─────────────────────────────────────────────────────

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
        ['codeReviewer', 'Review'],
        ['securityReviewer', 'Security'],
        ['buildPassed', 'Build'],
      ];

      const status = stepNames
        .map(([k, v]) => `${v} ${steps[k] ? '✓' : '✗'}`)
        .join(', ');

      workflowInfo = `${files.length} tracked files | ${status}`;
    }
  } catch { /* state file corrupt */ }
}

log(`[Context] Workflow: ${workflowInfo}`);

// ── 4. Active plan ────────────────────────────────────────────────────────

const plansDir = resolve(PROJECT_ROOT, '.claude', 'plans');
let activePlan = null;

if (existsSync(plansDir)) {
  try {
    const planFiles = readdirSync(plansDir)
      .filter((f) => f.endsWith('.md'))
      .map((f) => ({
        name: f,
        path: resolve(plansDir, f),
        mtime: statSync(resolve(plansDir, f)).mtimeMs,
      }))
      .sort((a, b) => b.mtime - a.mtime);

    if (planFiles.length > 0) {
      activePlan = planFiles[0];
    }
  } catch { /* ignore */ }
}

if (activePlan) {
  log(`[Context] Active Plan: .claude/plans/${activePlan.name}`);
  const planSummary = readFirstNLines(activePlan.path, 50);
  if (planSummary) {
    log('[Plan Summary]');
    log(planSummary);
    log('');
  }
}

// ── 5. Findings summary (from global pre-compact-context) ────────────────

const findingsPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'findings.md');
if (existsSync(findingsPath)) {
  const findings = readFileSync(findingsPath, 'utf-8');
  if (findings.trim().split('\n').length > 3) {
    log('[Findings Summary]');
    log(readFirstNLines(findingsPath, 20));
    log('');
  }
}

// ── 6. Progress summary (from global pre-compact-context) ────────────────

const progressPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'progress.md');
if (existsSync(progressPath)) {
  const progress = readFileSync(progressPath, 'utf-8');
  if (progress.trim().split('\n').length > 3) {
    log('[Progress Summary]');
    log(readFirstNLines(progressPath, 15));
    log('');
  }
}

process.exit(0);
