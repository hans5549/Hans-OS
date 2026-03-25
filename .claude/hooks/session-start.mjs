// ============================================================================
// session-start.mjs - SessionStart Hook: Welcome Banner + Workflow Status
// ============================================================================
// Resets planning phase steps each session to ensure independent plan reviews.
// ============================================================================

import { readFileSync, writeFileSync, existsSync, rmdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const STATE_FILE = resolve(PROJECT_ROOT, '.claude', 'workflow', 'state.json');
const LOCK_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow', '.build-lock');

const log = (msg) => process.stderr.write(msg + '\n');

// ── Clean up stale build lock from previous session ────────────────────────

if (existsSync(LOCK_DIR)) {
  try {
    rmdirSync(LOCK_DIR);
    log('[Cleanup] Removed stale .build-lock from previous session');
  } catch (err) {
    log(`[Cleanup] Failed to remove stale .build-lock: ${err.message}`);
  }
}

// ── Reset planning phase steps for new session ─────────────────────────────

if (existsSync(STATE_FILE)) {
  try {
    const state = JSON.parse(readFileSync(STATE_FILE, 'utf-8'));
    if (state.completedSteps) {
      state.completedSteps.ceoReview = false;
      state.completedSteps.engReview = false;
      state.completedSteps.planLinusReview = false;
      state.currentPlanFile = '';

      // Fix: Reset coding steps if previous session completed (modifiedFiles empty)
      // Skip reset during merge-back flow (mergeBackPending preserves state)
      if ((!state.modifiedFiles || state.modifiedFiles.length === 0) && !state.mergeBackPending) {
        state.completedSteps.simplifier = false;
        state.completedSteps.codeReviewer = false;
        state.completedSteps.securityReviewer = false;
        state.completedSteps.buildPassed = false;
        state.lineChangeSinceReview = 0;
        state.buildRetryCount = 0;
      }

      state.lastModified = new Date().toISOString();
      writeFileSync(STATE_FILE, JSON.stringify(state, null, 2), 'utf-8');
    }
  } catch { /* non-critical */ }
}

// ── Welcome banner ─────────────────────────────────────────────────────────

log('');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log(' WORKFLOW AUTOMATION ACTIVE');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log('');
log(' Coding: Simplifier → Code Review + Security → Build → Commit');
log(' Planning: CEO Review → Eng Review → Linus Review → ExitPlanMode');
log(' Commands: workflow status | workflow reset | /resume | /reboot-check');

// ── Worktree awareness ────────────────────────────────────────────────────
try {
  const wtOutput = execSync('git worktree list --porcelain', {
    encoding: 'utf-8',
    stdio: ['pipe', 'pipe', 'pipe'],
    cwd: PROJECT_ROOT,
  });
  const worktreeCount = (wtOutput.match(/^worktree /gm) || []).length;
  if (worktreeCount > 1) {
    log(` [Worktree] ${worktreeCount - 1} additional worktree(s) active`);
  }
} catch { /* git not available */ }

// ── Merge-back awareness ────────────────────────────────────────────────
try {
  if (existsSync(STATE_FILE)) {
    const stateContent = readFileSync(STATE_FILE, 'utf-8');
    const mbState = JSON.parse(stateContent);
    if (mbState.mergeBackPending === true) {
      log(' [!] MERGE-BACK PENDING — Run "worktree merge-back" after reviews');
    }
  }
} catch { /* state file doesn't exist or parse error */ }

// ── Auto-create findings.md and progress.md ─────────────────────────────

const workflowDir = resolve(PROJECT_ROOT, '.claude', 'workflow');
const findingsPath = resolve(workflowDir, 'findings.md');
const progressPath = resolve(workflowDir, 'progress.md');

if (!existsSync(findingsPath)) {
  try {
    writeFileSync(findingsPath, '# Research Findings\n\n> Research scratchpad. Auto-created by session-start hook.\n\n');
    log(' [Session] Created .claude/workflow/findings.md');
  } catch { /* non-critical */ }
}
if (!existsSync(progressPath)) {
  try {
    writeFileSync(progressPath, '# Progress Log\n\n> Cross-session progress log. Auto-updated by hooks.\n\n');
    log(' [Session] Created .claude/workflow/progress.md');
  } catch { /* non-critical */ }
}

// Show findings summary if has prior content
if (existsSync(findingsPath)) {
  try {
    const content = readFileSync(findingsPath, 'utf-8').trim();
    if (content.split('\n').length > 3) {
      log(' [Session] findings.md has prior research — consider reviewing');
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
      log(' [Recovery] Use /resume for full recovery, or continue with a new task');
    }
  } catch { /* ignore */ }
}

// ── Show workflow state if exists ──────────────────────────────────────────

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
        ['simplifier', 'Simplifier'],
        ['codeReviewer', 'Code Review'],
        ['securityReviewer', 'Security'],
        ['buildPassed', 'Build'],
      ];

      const done = stepNames.filter(([k]) => steps[k]).map(([, v]) => v);
      const pending = stepNames.filter(([k]) => !steps[k]).map(([, v]) => v);

      if (done.length > 0) log(`     Done: ${done.join(', ')}`);
      if (pending.length > 0) log(`     Pending: ${pending.join(', ')}`);
      log('     Use /resume to continue or workflow reset to start fresh');
    }
  } catch (err) {
    log(`[Hook] Failed to read workflow state: ${err.message}`);
  }
}

log('');
log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
log('');
