// ============================================================================
// pre-bash-check.mjs - PreToolUse Hook: Bash Interceptor + Commit Gate (v2)
// ============================================================================
// Protected file detection, git conventions, v2 5-gate commit gating with
// Findings Ledger validation.
//
// UPDATED (pipeline redesign):
// - Commit gate now validates: 5 gates complete + Codex verdict OK + all Code
//   phase ledger MEDIUM+ findings disposed + build passed
// - Uses commit-gate-validator.mjs helper for separation of concerns
// - Keeps: protected files, git merge --no-ff reminder, conventional commits,
//   final backend+frontend build, reset after success
// ============================================================================

import { execSync } from 'child_process';
import {
  getWorkflowState,
  resetStep,
  resetWorkflowState,
  isCodeFile,
} from './workflow-state.mjs';
import { validateCommitPreconditions } from './commit-gate-validator.mjs';
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
// 2. Get workflow state + check for tracked code files
// ============================================================================

const state = getWorkflowState();

let codeFilesExist = state.modifiedFiles.some((f) => isCodeFile(f));

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
  } catch { /* git not available */ }
}

if (!codeFilesExist) {
  log('');
  log('[Workflow] No code files detected - skipping workflow checks');
  log('');
  process.exit(0);
}

// ============================================================================
// 3. Commit gating — validate preconditions via helper
// ============================================================================

const result = validateCommitPreconditions();
if (!result.ok) {
  const message = `COMMIT BLOCKED - Workflow preconditions not met!\n\n${result.blocker}\n\n` +
                  `Commands:\n` +
                  `- 'workflow status'                          View current status + ledger progress\n` +
                  `- 'workflow override <target> <reason ≥ 20>' Emergency override\n`;
  process.stderr.write(message);
  process.exit(2);
}

// Show warnings (non-blocking)
if (result.warnings && result.warnings.length > 0) {
  log('');
  log('[Workflow] Commit allowed with warnings:');
  for (const w of result.warnings) {
    log(`  ⚠️  ${w}`);
  }
  log('');
}

// ============================================================================
// 4. Run final Build verification
// ============================================================================

log('');
log('[Workflow] All 5 gates completed + ledger disposed! Running final build verification...');
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

  const message = `FINAL BUILD FAILED (backend) - Cannot commit!\n\nBuild output:\n${output}\n\nPlease fix the build errors and try again.\nThe workflow will need to re-verify the build.`;
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

    const message = `FINAL BUILD FAILED (frontend) - Cannot commit!\n\nType check output:\n${output}\n\nPlease fix the type errors and try again.`;
    process.stderr.write(message);
    process.exit(2);
  }
}

// ============================================================================
// 5. Build success, allow commit
// ============================================================================

log('');
log('[Workflow] Final build passed!');
log('[Workflow] All workflow gates + build completed - commit allowed');

// Reset workflow state after successful commit
resetWorkflowState();

log('');
process.exit(0);
