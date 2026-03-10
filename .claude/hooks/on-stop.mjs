// ============================================================================
// on-stop.mjs - Stop Hook: Session Cleanup & Reminder
// ============================================================================
// On session end:
// 1. Clean up stale .build-lock directory
// 2. Remind about pending workflow steps (non-blocking)
// ============================================================================

import { existsSync, readFileSync, appendFileSync, rmdirSync } from 'fs';
import { execSync } from 'child_process';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { log } from './hook-utils.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

// ── Clean up stale build lock ──────────────────────────────────────────────

const LOCK_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow', '.build-lock');

if (existsSync(LOCK_DIR)) {
  try {
    rmdirSync(LOCK_DIR);
    log('[Cleanup] Removed stale .build-lock');
  } catch { /* ignore */ }
}

// ── Workflow reminder ──────────────────────────────────────────────────────

const STATE_FILE = resolve(PROJECT_ROOT, '.claude', 'workflow', 'state.json');

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));
    const files = state.modifiedFiles || [];
    const steps = state.completedSteps || {};

    if (files.length > 0) {
      const stepNames = [
        ['simplifier', 'Simplifier'],
        ['specCheck', 'Spec Check'],
        ['codeReviewer', 'Code Review'],
        ['securityReviewer', 'Security'],
        ['linusGreen', 'Linus'],
        ['buildPassed', 'Build'],
      ];

      const pending = stepNames.filter(([k]) => !steps[k]).map(([, v]) => v);

      if (pending.length > 0) {
        log('');
        log('[Session End] Workflow has pending steps:');
        log(`  Tracked files: ${files.length}`);
        log(`  Pending: ${pending.join(', ')}`);
        log('  Use /resume in next session to continue.');
        log('');
      }
    }
  } catch { /* ignore */ }
}

// ── Area 2c: Auto-write session summary to progress.md ───────────────────

const progressPath = resolve(PROJECT_ROOT, '.claude', 'workflow', 'progress.md');

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));

    let branch = '(unknown)';
    try {
      branch = execSync('git branch --show-current', {
        encoding: 'utf-8',
        stdio: ['pipe', 'pipe', 'pipe'],
        cwd: PROJECT_ROOT,
      }).trim();
    } catch { /* git not available */ }

    const completedStepNames = Object.entries(state.completedSteps || {})
      .filter(([, v]) => v)
      .map(([k]) => k);

    const summary = [
      `\n## Session: ${new Date().toISOString().slice(0, 16)}`,
      `- Branch: ${branch}`,
      `- Modified: ${(state.modifiedFiles || []).join(', ') || 'none'}`,
      `- Steps: ${completedStepNames.join(', ') || 'none'}`,
      `- Tool calls: ${state.toolCallCount || 0}`,
      `- Research ops: ${state.researchOpCount || 0}`,
      '',
    ].join('\n');

    appendFileSync(progressPath, summary);
    log('[Session] Progress auto-written to progress.md');
  } catch { /* non-critical */ }
}

process.exit(0);
