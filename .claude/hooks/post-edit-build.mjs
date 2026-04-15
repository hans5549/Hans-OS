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
import { getWorkflowState, setWorkflowState, completeStep } from './workflow-state.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

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

  // Mark buildPassed step complete for workflow status accuracy
  try { completeStep('buildPassed'); } catch { /* non-critical */ }

  // Reset retry count on success
  try {
    const state = getWorkflowState();
    if (state.buildRetryCount > 0) {
      state.buildRetryCount = 0;
      setWorkflowState(state);
    }
  } catch { /* non-critical */ }
} catch (err) {
  let output = (err.stdout || '') + (err.stderr || '');
  if (output.length > 600) output = output.substring(0, 600) + '...';
  log(`[Build] FAILED (${buildType}):\n${output}`);

  // Increment retry count + Strike warnings
  try {
    const state = getWorkflowState();
    state.buildRetryCount = (state.buildRetryCount || 0) + 1;

    if (state.buildRetryCount === 3) {
      log('');
      log('━━━━ Strike 2: Build failed 3 times ━━━━');
      log('Current approach may have a fundamental problem:');
      log('  1. STOP — analyze why the fixes are not working');
      log('  2. Try a completely different approach');
      log('  3. Document: what did the 3 attempts teach you?');
      log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    }

    if (state.buildRetryCount >= 5) {
      log('');
      log('━━━━ Strike 3: Build failed 5+ times ━━━━');
      log('Escalation threshold reached:');
      log('  1. STOP — do not retry the same direction');
      log('  2. Re-read relevant source (something may be missed)');
      log('  3. Report to user: what was tried + what was learned');
      log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    }

    setWorkflowState(state);
  } catch { /* non-critical */ }
} finally {
  try { rmdirSync(LOCK_DIR); } catch { /* ignore */ }
}

process.exit(0);
