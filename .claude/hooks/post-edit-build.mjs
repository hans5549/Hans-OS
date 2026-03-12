// ============================================================================
// post-edit-build.mjs - PostToolUse Hook: Auto Build on Code File Edit
// ============================================================================
// Dual build system for Hans-OS monorepo:
//   - Backend (.cs) → dotnet build backend/HansOS.slnx
//   - Frontend (.vue, .ts, .tsx) → cd frontend && pnpm check:type
// Uses atomic directory lock to prevent parallel builds.
// Tracks build retry count (max 5) in workflow state.
// ============================================================================

import { execSync } from 'child_process';
import { existsSync, mkdirSync, rmdirSync } from 'fs';
import { extname, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { parseHookInput, log } from './hook-utils.mjs';
import { getWorkflowState, setWorkflowState } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

// ── Area 2d: Reset research counter on Write/Edit (entering implementation mode) ─

{
  const state = getWorkflowState();
  if (state.researchOpCount > 0) {
    state.researchOpCount = 0;
    setWorkflowState(state);
  }
}

// ── Extract file path from tool input ──────────────────────────────────────

const toolName = parsed.tool_name;
const input = parsed.tool_input || {};

let filePath = null;
switch (toolName) {
  case 'Edit':
  case 'MultiEdit':
  case 'Write':
    filePath = input.file_path || null;
    break;
  case 'mcp__filesystem__edit_file':
  case 'mcp__filesystem__write_file':
    filePath = input.path || null;
    break;
}

if (!filePath) process.exit(0);

// ── Determine build type based on file extension ───────────────────────────

const BACKEND_EXTS = new Set(['.cs', '.razor']);
const FRONTEND_EXTS = new Set(['.vue', '.ts', '.tsx']);
const ext = extname(filePath).toLowerCase();

let buildType = null;
if (BACKEND_EXTS.has(ext)) buildType = 'backend';
else if (FRONTEND_EXTS.has(ext)) buildType = 'frontend';

if (!buildType) process.exit(0);

// ── Atomic lockfile using mkdirSync (directory creation is atomic) ────────

const LOCK_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow', '.build-lock');

try {
  mkdirSync(LOCK_DIR, { recursive: false });
} catch {
  log('[Build] Skipping — another build is in progress');
  process.exit(0);
}

// ── Run build ──────────────────────────────────────────────────────────────

try {
  if (buildType === 'backend') {
    const solutionPath = resolve(PROJECT_ROOT, 'backend', 'HansOS.slnx');
    log(`[Build] Auto-building backend after ${ext} edit...`);

    execSync(`dotnet build "${solutionPath}" --no-restore -v q`, {
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
      timeout: 60000,
      cwd: PROJECT_ROOT,
    });

    log('[Build] Backend build passed');
  } else {
    log(`[Build] Auto-checking frontend types after ${ext} edit...`);

    execSync('pnpm check:type', {
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
      timeout: 60000,
      cwd: resolve(PROJECT_ROOT, 'frontend'),
    });

    log('[Build] Frontend type check passed');
  }

  // Reset retry count on success + record edit history
  try {
    const state = getWorkflowState();
    if (state.buildRetryCount > 0) {
      state.buildRetryCount = 0;
    }
    // Area 3: Record successful edit
    if (!Array.isArray(state.editHistory)) state.editHistory = [];
    state.editHistory.push({ file: filePath, time: Date.now(), buildOk: true });
    if (state.editHistory.length > 10) state.editHistory = state.editHistory.slice(-10);
    setWorkflowState(state);
  } catch { /* non-critical */ }
} catch (err) {
  let output = (err.stdout || '') + (err.stderr || '');
  if (output.length > 600) output = output.substring(0, 600) + '...';
  log(`[Build] FAILED (${buildType}):\n${output}`);

  // Increment retry count + Area 3: Strike tracking
  try {
    const state = getWorkflowState();
    state.buildRetryCount = (state.buildRetryCount || 0) + 1;

    // Area 3: Record failed edit
    if (!Array.isArray(state.editHistory)) state.editHistory = [];
    state.editHistory.push({ file: filePath, time: Date.now(), buildOk: false });
    if (state.editHistory.length > 10) state.editHistory = state.editHistory.slice(-10);

    // Count consecutive failures on the same file
    const recentFails = [];
    for (let i = state.editHistory.length - 1; i >= 0; i--) {
      const entry = state.editHistory[i];
      if (entry.file === filePath && !entry.buildOk) recentFails.push(entry);
      else break;
    }

    if (recentFails.length === 3) {
      log('');
      log('━━━━ Strike 2: Same file failed build 3 times ━━━━');
      log('Current approach may have a fundamental problem:');
      log('  1. STOP — analyze why the fixes are not working');
      log('  2. Try a completely different approach');
      log('  3. Document: what did the 3 attempts teach you?');
      log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    }

    if (recentFails.length >= 5) {
      log('');
      log('━━━━ Strike 3: Same file failed build 5+ times ━━━━');
      log('Escalation threshold reached:');
      log('  1. STOP — do not retry the same direction');
      log('  2. Re-read relevant source (something may be missed)');
      log('  3. Report to user: what was tried + what was learned');
      log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    }

    setWorkflowState(state);

    if (state.buildRetryCount >= 5) {
      log('');
      log('[Build] WARNING: Build has failed 5+ times consecutively.');
      log('[Build] STOP retrying. Analyze the root cause and report to the user.');
      log('');
    }
  } catch { /* non-critical */ }
} finally {
  try { rmdirSync(LOCK_DIR); } catch { /* ignore */ }
}

process.exit(0);
