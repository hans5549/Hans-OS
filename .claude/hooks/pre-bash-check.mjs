// ============================================================================
// pre-bash-check.mjs - PreToolUse Hook: Bash Command Interceptor & Commit Gate
// ============================================================================
// Intercepts dangerous commands, validates git conventions, checks workflow.
// NOTE: EF Core migrations are ALLOWED in this project (Code-First).
// ============================================================================

import { execSync } from 'child_process';
import {
  getWorkflowState,
  isCodeFile,
  resetStep,
  resetWorkflowState,
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
// 0. Dangerous command blocking (before commit check)
// ============================================================================

const BLOCKED_COMMANDS = [
  {
    pattern: /git\s+add\s+(-A\b|--all\b|\.(\s|"|'|$))/i,
    message: `BLOCKED: 'git add .' and 'git add -A' are forbidden.
Stage specific files only: git add file1.cs file2.cs
Use 'git status' to review first.`,
  },
];

for (const { pattern, message } of BLOCKED_COMMANDS) {
  if (pattern.test(command)) {
    process.stderr.write(message);
    process.exit(2);
  }
}

// ============================================================================
// 0.3 Protected file modification via Bash
// ============================================================================

const PROTECTED_FILE_PATTERNS = [
  /\.claude\/hooks\//i,
  /\.claude\/workflow\/state\.json/i,
  /\.claude\/settings\.local\.json/i,
];

if (PROTECTED_FILE_PATTERNS.some(p => p.test(command))) {
  // Allow read-only operations (cat, type, more, head, tail, node --check, git diff, etc.)
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

const msgMatch = command.match(/-m\s+(?:"([^"]*(?:"[^"]*"[^"]*)*)"|'([^']*)'|"?\$\(cat\s+<<)/);
if (msgMatch) {
  const msg = msgMatch[1] || msgMatch[2] || '';
  if (msg) {
    const conventionalPattern = /^(feat|fix|refactor|docs|perf|build|chore|ci|style|test)(\(.+\))?: .+/;
    if (!conventionalPattern.test(msg)) {
      log('');
      log('[Git] WARNING: Commit message does not follow Conventional Commits format.');
      log('[Git] Expected: <type>(scope): description');
      log('[Git] Types: feat, fix, refactor, docs, perf, build, chore, ci, style, test');
      log('[Git] Example: feat(auth): 新增 JWT refresh token 自動續期機制');
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
// 4. Check if required steps are complete
// ============================================================================

const requiredSteps = [
  { name: 'simplifier', display: 'Code Simplifier' },
  { name: 'codeReviewer', display: 'Code Review' },
  { name: 'securityReviewer', display: 'Security Review' },
];

const missingSteps = requiredSteps.filter((s) => !state.completedSteps[s.name]);

if (missingSteps.length > 0) {
  const stepList = missingSteps.map((s) => `  [ ] ${s.display}`).join('\n');

  const message = `COMMIT BLOCKED - Workflow steps incomplete!

Missing steps:
${stepList}

Please complete the following workflow before committing:
1. Run code-simplifier agent (auto-completed by hook)
2. Run code-review-specialist agent (auto-completed by hook)
3. Run security-vuln-scanner agent (auto-completed by hook)
Final build verification runs automatically on commit.

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
    cwd: process.env.PROJECT_ROOT || undefined,
  });
  log('[Build] Backend build passed');
} catch (err) {
  // Build failed
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
log('');

// Reset workflow state (prepare for next round)
resetWorkflowState();

process.exit(0);
