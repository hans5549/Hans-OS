// ============================================================================
// pre-bash-check.mjs - PreToolUse Hook: Bash Command Interceptor & Commit Gate
// ============================================================================
// Protected file detection, git conventions, tiered commit gating.
// Dangerous command blocking (EF migrations, git add .) moved to deny permissions.
// ============================================================================

import { execSync } from 'child_process';
import {
  getWorkflowState,
  setWorkflowState,
  isCodeFile,
  resetStep,
  resetWorkflowState,
  getCodingMissingSteps,
} from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const toolName = parsed.tool_name;
if (toolName !== 'Bash') process.exit(0);

const command = (parsed.tool_input && parsed.tool_input.command) || '';
if (!command) process.exit(0);

// ============================================================================
// 0. Protected file modification via Bash
// ============================================================================

const PROTECTED_FILE_PATTERNS = [
  /\.claude\/hooks\//i,
  /\.claude\/workflow\/state\.json/i,
  /\.claude\/settings\.local\.json/i,
];

if (PROTECTED_FILE_PATTERNS.some(p => p.test(command))) {
  const isReadOnly = /^(cat|type|more|head|tail|node\s+--check|git\s+(diff|log|show))\b/i.test(command.trim());
  if (!isReadOnly) {
    process.stderr.write(
      'BLOCKED: This command targets a protected workflow file.\n' +
      'Hook files, workflow state, and settings cannot be modified via Bash.\n' +
      'If you need to modify hooks, ask the user to do it manually.'
    );
    process.exit(2);
  }
}

// ============================================================================
// 0.5 Git merge --no-ff reminder
// ============================================================================

if (/git\s+merge\b/i.test(command) && !/--no-ff/i.test(command)) {
  log('[Git] Reminder: Always use --no-ff to preserve branch history.');
  log('[Git] Example: git merge --no-ff feature/xxx');
}

// ============================================================================
// 1. Check if git commit command
// ============================================================================

const isCommit = /git\s+commit/i.test(command);
if (!isCommit) process.exit(0);

// ============================================================================
// 1.5 Conventional Commits format validation
// ============================================================================

const isHeredoc = /\$\(cat\s+<</.test(command);
const msgMatch = command.match(/-m\s+(?:"([^"]*(?:"[^"]*"[^"]*)*)"|'([^']*)')/);
if (!isHeredoc && msgMatch) {
  const msg = msgMatch[1] || msgMatch[2] || '';
  if (msg) {
    const conventionalPattern = /^(feat|fix|refactor|docs|perf|build|chore|ci|style|test)(\(.+\))?: .+/;
    if (!conventionalPattern.test(msg)) {
      log('');
      log('[Git] WARNING: Commit message does not follow Conventional Commits format.');
      log('[Git] Expected: <type>(scope): description');
      log('[Git] Types: feat, fix, refactor, docs, perf, build, chore, ci, style, test');
      log('[Git] Example: feat(settings): add incremental build checks');
      log('');
    }
  }
}

// ============================================================================
// 2. Get workflow state
// ============================================================================

const state = getWorkflowState();

// ============================================================================
// 3. Check for tracked code files
// ============================================================================

let codeFilesExist = state.modifiedFiles.some((f) => isCodeFile(f));

// If no tracked code files, check git staged files
if (!codeFilesExist && state.modifiedFiles.length === 0) {
  try {
    const staged = execSync('git diff --cached --name-only', { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
    if (staged) {
      const codeExts = new Set(['.cs', '.razor', '.json', '.css', '.js', '.html', '.csproj', '.xml', '.jsx', '.ts', '.tsx', '.vue', '.mts']);
      const { extname } = await import('path');
      codeFilesExist = staged.split('\n')
        .filter(f => !f.startsWith('.claude/'))
        .some((f) => codeExts.has(extname(f).toLowerCase()));
    }
  } catch {
    // Git not available
  }
}

// Pure doc modification, skip workflow checks
if (!codeFilesExist) {
  log('');
  log('[Workflow] No code files detected - skipping workflow checks');
  log('');
  process.exit(0);
}

// ============================================================================
// 4. Commit gating — all code changes require full pipeline
// ============================================================================

const missingSteps = getCodingMissingSteps();

if (missingSteps.length > 0) {
  const stepDisplayNames = {
    simplifier: 'Code Simplifier (run code-simplifier:code-simplifier agent)',
    codeReviewer: 'Code Review (run code-review-specialist agent)',
    securityReviewer: 'Security Review (run security-vuln-scanner agent)',
  };

  const stepList = missingSteps.map((s) => `  [ ] ${stepDisplayNames[s] || s}`).join('\n');

  const message = `COMMIT BLOCKED - Workflow steps incomplete!

Missing steps:
${stepList}

Complete these steps, then commit will be allowed.

Commands:
- 'workflow status' - View current status
- 'workflow reset' - Reset all steps
- 'workflow skip <step>' - Skip a step (not recommended)`;

  process.stderr.write(message);
  process.exit(2);
}

// ============================================================================
// 5. All steps complete, run final Build verification
// ============================================================================

log('');
log('[Workflow] All steps completed! Running final build verification...');
log('');

// Backend build
const solutionPath = 'backend/HansOS.slnx';
log(`[Build] Building backend: ${solutionPath}`);

try {
  execSync(`dotnet build ${solutionPath}`, {
    encoding: 'utf-8',
    stdio: ['pipe', 'pipe', 'pipe'],
    timeout: 120000,
  });
  log('[Build] Backend build passed');
} catch (err) {
  resetStep('buildPassed');

  let output = (err.stdout || '') + (err.stderr || '');
  if (output.length > 800) output = output.substring(0, 800) + '...';

  const message = `FINAL BUILD FAILED (backend) - Cannot commit!

Build output:
${output}

Please fix the build errors and try again.
The workflow will need to re-verify the build.`;

  process.stderr.write(message);
  process.exit(2);
}

// Frontend type check (if frontend files were modified)
const hasFrontendFiles = state.modifiedFiles.some((f) => {
  const e = f.split('.').pop().toLowerCase();
  return ['vue', 'ts', 'tsx', 'mts'].includes(e);
});

if (hasFrontendFiles) {
  log('[Build] Checking frontend types...');
  try {
    execSync('pnpm check:type', {
      encoding: 'utf-8',
      stdio: ['pipe', 'pipe', 'pipe'],
      timeout: 120000,
      cwd: 'frontend',
    });
    log('[Build] Frontend type check passed');
  } catch (err) {
    resetStep('buildPassed');

    let output = (err.stdout || '') + (err.stderr || '');
    if (output.length > 800) output = output.substring(0, 800) + '...';

    const message = `FINAL BUILD FAILED (frontend) - Cannot commit!

Type check output:
${output}

Please fix the type errors and try again.`;

    process.stderr.write(message);
    process.exit(2);
  }
}

// ============================================================================
// 6. Build success, allow commit
// ============================================================================

log('');
log('[Workflow] Final build passed!');
log('[Workflow] All workflow steps completed - commit allowed');

// Detect worktree and handle merge-back flow
let isInWorktree = false;
try {
  const gitDir = execSync('git rev-parse --git-dir', {
    encoding: 'utf-8',
    stdio: ['pipe', 'pipe', 'pipe'],
  }).trim();
  isInWorktree = gitDir.includes('.git/worktrees/') || gitDir.includes('.git\\worktrees\\');
} catch { /* not in git repo */ }

if (isInWorktree) {
  const mbState = getWorkflowState();

  if (!mbState.mergeBackPending) {
    // First commit in worktree — enter merge-back flow
    mbState.mergeBackPending = true;
    mbState.completedSteps.simplifier = false;
    mbState.completedSteps.codeReviewer = false;
    mbState.completedSteps.securityReviewer = false;
    mbState.completedSteps.buildPassed = false;
    mbState.modifiedFiles = [];
    mbState.editHistory = [];
    mbState.lineChangeSinceReview = 0;
    mbState.buildRetryCount = 0;
    setWorkflowState(mbState);

    let wtBranch = '';
    try {
      wtBranch = execSync('git branch --show-current', {
        encoding: 'utf-8',
        stdio: ['pipe', 'pipe', 'pipe'],
      }).trim();
    } catch { /* fallback */ }

    log('');
    log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    log(' COMMIT SUCCESSFUL — MERGE-BACK FLOW STARTED');
    log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    log(` Branch: ${wtBranch}`);
    log('');
    log(' Next steps:');
    log('   1. git merge --no-ff main');
    log('   2. Resolve conflicts if any, then commit');
    log('   3. code-review (re-run review pipeline)');
    log('   4. worktree merge-back (finalize)');
    log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  } else {
    // Subsequent commit while mergeBackPending (e.g. after merge conflict resolution)
    resetWorkflowState();
  }
} else {
  // Not in worktree — original behavior
  resetWorkflowState();
}

log('');
process.exit(0);
