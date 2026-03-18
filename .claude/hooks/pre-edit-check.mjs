// ============================================================================
// pre-edit-check.mjs - PreToolUse Hook: File Edit Tracker
// ============================================================================
// Tracks code file modifications, maintains main branch protection,
// records currentPlanFile when .claude/plans/*.md is written.
// Supports: Edit, MultiEdit, Write, mcp__filesystem__edit_file, mcp__filesystem__write_file
// ============================================================================

import { execSync } from 'child_process';
import { getWorkflowState, setWorkflowState, addModifiedFile, isCodeFile, isDocFile } from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const toolName = parsed.tool_name;
if (!toolName) process.exit(0);

// Check if this is a tool we want to track
const trackedTools = ['Edit', 'MultiEdit', 'Write', 'mcp__filesystem__edit_file', 'mcp__filesystem__write_file'];
if (!trackedTools.includes(toolName)) {
  process.exit(0);
}

// ============================================================================
// 1. Main Branch Protection
// ============================================================================

try {
  const branch = execSync('git branch --show-current', { encoding: 'utf-8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
  if (branch === 'main' || branch === 'master') {
    const blockMessage = JSON.stringify({
      block: true,
      message: 'BLOCKED: Cannot edit files on main branch. Create a feature branch first:\n\n  git checkout -b feature/xxx\n  or\n  git worktree add ../worktrees/<name> -b feature/xxx',
    });
    process.stdout.write(blockMessage);
    process.exit(2);
  }
} catch {
  // Git not available, allow operation
}

// ============================================================================
// 1.5. Protected Files — prevent Claude from modifying hooks/state/settings
// ============================================================================

{
  const ti = parsed.tool_input || {};
  const targetPath = (ti.file_path || ti.path || '').replace(/\\/g, '/');
  const PROTECTED_PATTERNS = [
    /\.claude\/hooks\//i,
    /\.claude\/workflow\/state\.json$/i,
    /\.claude\/settings\.local\.json$/i,
  ];

  if (PROTECTED_PATTERNS.some(p => p.test(targetPath))) {
    process.stderr.write(
      `BLOCKED: "${targetPath}" is a protected workflow file.\n` +
      `Hook files, workflow state, and settings cannot be modified during a session.\n` +
      `If you need to modify hooks, ask the user to do it manually.`
    );
    process.exit(2);
  }
}

// ============================================================================
// 2. Extract File Path (MUST come before Sensitive File Warning)
// ============================================================================

let filePath = null;
const input = parsed.tool_input || {};

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

// ============================================================================
// 2.5. Sensitive File Warning
// ============================================================================

{
  const fp = input.file_path || input.path || '';
  if (fp && /appsettings.*\.json$/i.test(fp)) {
    log('[WARNING] Editing appsettings.json — connection strings, JWT keys should be set via environment variables.');
  }
}

// ============================================================================
// 3. Record currentPlanFile when writing to .claude/plans/*.md
// ============================================================================

if (filePath) {
  const normalized = filePath.replace(/\\/g, '/');
  if (/\.claude\/plans\/.*\.md$/i.test(normalized)) {
    const state = getWorkflowState();
    state.currentPlanFile = normalized;
    setWorkflowState(state);
    log(`[Workflow] Plan file recorded: ${normalized}`);
  }
}

// ============================================================================
// 4. Track File Modification
// ============================================================================

if (filePath) {
  const isCode = isCodeFile(filePath);
  const isDoc = isDocFile(filePath);

  if (isCode || isDoc) {
    // Estimate line count from edit (rough: count newlines in new_string)
    let lineCount = 0;
    if (input.new_string) {
      lineCount = (input.new_string.match(/\n/g) || []).length;
    } else if (input.content) {
      lineCount = (input.content.match(/\n/g) || []).length;
    }

    const state = addModifiedFile(filePath, lineCount);

    if (isCode) {
      const codeCount = state.modifiedFiles.filter((f) => isCodeFile(f)).length;
      const cumLines = state.lineChangeSinceReview || 0;

      if (state.completedSteps.simplifier && cumLines > 0 && cumLines < 10) {
        log(`[Workflow] Tracking code edit: ${filePath} (+${lineCount} lines, cumulative ${cumLines}/10 — reviews preserved)`);
      } else {
        log(`[Workflow] Tracking code file: ${filePath} | Total code files: ${codeCount}`);
        if (!state.completedSteps.simplifier || cumLines >= 10) {
          log('[Workflow] Review steps have been reset');
        }
      }
    }
  }
}

// Allow operation to continue
process.exit(0);
