// ============================================================================
// session-start.mjs - SessionStart Hook (v2)
// ============================================================================
// Resets planning phase steps each session, shows banner, restores progress.
//
// UPDATED (pipeline redesign):
// - Resets all 4 plan reviewers (ceo/eng/planLinus/planCodex)
// - Resets 5 coding gates when previous session completed
// - NEW: Checks Codex stop-review-gate status, warns if enabled
// - Removed: findings.md auto-create (dead code; replaced by deferred.md elsewhere)
// - Updated banner text to reflect 5-gate pipeline
// ============================================================================

import { readFileSync, writeFileSync, existsSync, rmdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const STATE_FILE = resolve(PROJECT_ROOT, '.claude', 'workflow', 'state.json');
const LOCK_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow', '.build-lock');

const log = (msg) => process.stderr.write(msg + '\n');

// ── Clean up stale build lock ──────────────────────────────────────────────

if (existsSync(LOCK_DIR)) {
  try {
    rmdirSync(LOCK_DIR);
    log('[Cleanup] Removed stale .build-lock from previous session');
  } catch (err) {
    log(`[Cleanup] Failed to remove stale .build-lock: ${err.message}`);
  }
}

// ── Reset planning phase steps for new session (4 reviewers) ───────────────

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));
    if (state.completedSteps) {
      // Reset all 4 plan reviewers
      state.completedSteps.ceoReview = false;
      state.completedSteps.engReview = false;
      state.completedSteps.planLinusReview = false;
      state.completedSteps.planCodexReview = false;
      state.completedSteps.planCodexVerdict = null;
      state.currentPlanFile = '';

      // Clear single-use overrides (unblock-next)
      if (state.overrides && state.overrides['unblock-next']) {
        state.overrides['unblock-next'].active = false;
      }

      // Reset coding phase if previous session completed (no tracked files)
      if (!state.modifiedFiles || state.modifiedFiles.length === 0) {
        state.completedSteps.gateSafetyDone = false;
        state.completedSteps.gateProjectFitDone = false;
        state.completedSteps.gateTasteDone = false;
        state.completedSteps.gateCleanupDone = false;
        state.completedSteps.gateXCodexDone = false;
        state.completedSteps.gateXCodexVerdict = null;
        state.completedSteps.buildPassed = false;
        state.lineChangeSinceReview = 0;
        state.buildRetryCount = 0;
        // Also clear per-session overrides
        state.overrides = {};
      }

      state.lastModified = new Date().toISOString();
      writeFileSync(STATE_FILE, JSON.stringify(state, null, 2), 'utf-8');
    }
  } catch { /* non-critical */ }
}

// ── Welcome banner ─────────────────────────────────────────────────────────

log('');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log(' WORKFLOW AUTOMATION ACTIVE (v2 — mission-based 5-gate)');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log('');
log(' Planning: CEO / Eng / plan-Linus / plan-Codex (4 parallel) → ExitPlanMode');
log(' Coding:   Gate A Safety → B Project Fit → C Taste → D Cleanup → X Codex → Build → Commit');
log(' Commands: workflow status | workflow reset | workflow override <target> <reason>');

// ── Check Codex stop-review-gate status ───────────────────────────────────

try {
  const home = process.env.HOME || process.env.USERPROFILE || '';
  if (home) {
    const codexConfigPath = resolve(home, '.codex', 'config.json');
    if (existsSync(codexConfigPath)) {
      const codexConfig = JSON.parse(readFileSync(codexConfigPath, 'utf-8'));
      if (codexConfig.stopReviewGate === true) {
        log('');
        log(' ⚠️  Codex stop-review-gate is ON');
        log('    Stop may be blocked if Codex finds issues in last-turn edits.');
        log('    Monitor OpenAI quota. Disable: /codex:setup --disable-review-gate');
      }
    }
  }
} catch { /* non-critical */ }

// ── Auto-create progress.md if missing ─────────────────────────────────────

const workflowDir = resolve(PROJECT_ROOT, '.claude', 'workflow');
const progressPath = resolve(workflowDir, 'progress.md');

if (!existsSync(progressPath)) {
  try {
    writeFileSync(progressPath, '# Progress Log\n\n> Cross-session progress log. Auto-updated by on-stop hook.\n\n');
    log('');
    log(' [Session] Created .claude/workflow/progress.md');
  } catch { /* non-critical */ }
}

// ── Show deferred findings count ──────────────────────────────────────────

const deferredPath = resolve(workflowDir, 'deferred.md');
if (existsSync(deferredPath)) {
  try {
    const deferred = readFileSync(deferredPath, 'utf-8');
    const openEntries = (deferred.match(/^## entry-\d+[\s\S]*?Status:\s*open/gm) || []).length;
    if (openEntries > 0) {
      log('');
      log(` [Deferred] ${openEntries} finding(s) awaiting future handling in deferred.md`);
    }
  } catch { /* ignore */ }
}

// ── Session recovery — show last session summary ─────────────────────────

if (existsSync(progressPath)) {
  try {
    const lines = readFileSync(progressPath, 'utf-8').split('\n');
    let lastSessionStart = -1;
    for (let i = lines.length - 1; i >= 0; i--) {
      if (lines[i].startsWith('## Session:')) {
        lastSessionStart = i;
        break;
      }
    }
    if (lastSessionStart >= 0) {
      const lastSession = lines.slice(lastSessionStart, lastSessionStart + 6).join('\n');
      log('');
      log(' [Recovery] Previous session:');
      for (const line of lastSession.split('\n').slice(0, 5)) {
        log(`   ${line}`);
      }
      log(' [Recovery] Review progress.md for details, or continue with a new task');
    }
  } catch { /* ignore */ }
}

// ── Show workflow state if in-progress ────────────────────────────────────

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));
    const files = state.modifiedFiles || [];
    const steps = state.completedSteps || {};

    if (files.length > 0) {
      log('');
      log(' [!] Previous workflow in progress:');
      log(`     Tracked files: ${files.length}`);

      const stepNames = [
        ['gateSafetyDone', 'Gate A'],
        ['gateProjectFitDone', 'Gate B'],
        ['gateTasteDone', 'Gate C'],
        ['gateCleanupDone', 'Gate D'],
        ['gateXCodexDone', 'Gate X'],
        ['buildPassed', 'Build'],
      ];

      const done = stepNames.filter(([k]) => steps[k]).map(([, v]) => v);
      const pending = stepNames.filter(([k]) => !steps[k]).map(([, v]) => v);

      if (done.length > 0) log(`     Done:    ${done.join(', ')}`);
      if (pending.length > 0) log(`     Pending: ${pending.join(', ')}`);
      log('     Use "workflow status" to review or "workflow reset" to start fresh');
    }
  } catch (err) {
    log(`[Hook] Failed to read workflow state: ${err.message}`);
  }
}

log('');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log('');
