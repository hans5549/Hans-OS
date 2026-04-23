// ============================================================================
// workflow-orchestrator.mjs - UserPromptSubmit Hook: Workflow Controller (v2)
// ============================================================================
// Detects user input and manages workflow state:
// - Detects step completion signals -> updates state
// - Detects commit intent -> shows pending checklist
// - Supports workflow status/reset/skip commands
// - NEW: workflow override <target> <reason> universal override command
// - NEW: code-review / commit this use 5-gate sequence
// - NEW: Plan phase guidance mentions 4 reviewers (adds plan-codex-adversarial-reviewer)
// ============================================================================

import { readFileSync, appendFileSync, existsSync, mkdirSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import {
  getWorkflowState,
  setWorkflowState,
  resetWorkflowState,
  completeStep,
  isCodeFile,
  showWorkflowStatus,
} from './workflow-state.mjs';
import { allCodingGatesComplete, getCodingMissingGates } from './workflow-gates.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');
const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');

// ── Main ───────────────────────────────────────────────────────────────────

const parsed = await parseHookInput();
let prompt;

if (parsed) {
  prompt = parsed.prompt || '';
} else {
  process.exit(0);
}

if (!prompt) process.exit(0);

const promptLower = prompt.toLowerCase();

// ============================================================================
// 1. Universal override command: workflow override <target> <reason>
// ============================================================================

const overrideMatch = prompt.match(/workflow\s+override\s+(\w+(?:-\w+)*)\s+(.+)/i);
if (overrideMatch) {
  const target = overrideMatch[1];
  const reason = overrideMatch[2].trim();

  const validTargets = ['gateA', 'gateB', 'gateC', 'gateD', 'gateX', 'planCodex', 'unblock-next'];
  if (!validTargets.includes(target)) {
    log('');
    log(`[Workflow] ERROR: Invalid override target "${target}".`);
    log(`[Workflow] Valid targets: ${validTargets.join(', ')}`);
    log('');
    process.exit(0);
  }
  if (reason.length < 20) {
    log('');
    log(`[Workflow] ERROR: Override reason must be ≥ 20 characters (got ${reason.length}).`);
    log('[Workflow] Be specific about why the override is needed. Examples:');
    log('[Workflow]   "Codex CLI quota exhausted, monthly reset on 2026-05-01"');
    log('[Workflow]   "false positive about logging pattern — verified against CLAUDE.md"');
    log('');
    process.exit(0);
  }

  const state = getWorkflowState();
  state.overrides = state.overrides || {};
  const timestamp = new Date().toISOString();
  if (target === 'unblock-next') {
    state.overrides[target] = { active: true, reason, timestamp };
  } else {
    state.overrides[target] = { reason, timestamp };
  }
  setWorkflowState(state);

  // Append to skip-log.md
  try {
    if (!existsSync(WORKFLOW_DIR)) mkdirSync(WORKFLOW_DIR, { recursive: true });
    const logPath = resolve(WORKFLOW_DIR, 'skip-log.md');
    const logEntry = `\n## ${timestamp} — override ${target}\nReason: ${reason}\n`;
    appendFileSync(logPath, logEntry);
  } catch { /* non-critical */ }

  log('');
  log(`[Workflow] Override registered: ${target}`);
  log(`[Workflow]   Reason: ${reason.slice(0, 80)}${reason.length > 80 ? '...' : ''}`);
  log(`[Workflow]   Commit message will include: Override-${target}-Reason: <reason>`);
  log(`[Workflow]   Record written to .claude/workflow/skip-log.md`);
  log('');
  process.exit(0);
}

// ============================================================================
// 2. Workflow commands
// ============================================================================

// workflow status
if (/workflow\s+status/i.test(promptLower)) {
  showWorkflowStatus();
  process.exit(0);
}

// workflow reset
if (/workflow\s+reset/i.test(promptLower)) {
  resetWorkflowState();
  log('');
  log('[Workflow] State has been reset!');
  log('');
  process.exit(0);
}

// workflow skip <step> (legacy, kept for backward compat with existing muscle memory)
const skipMatch = promptLower.match(/workflow\s+skip\s+(\w+)/);
if (skipMatch && !overrideMatch) {
  const stepToSkip = skipMatch[1];

  const stepMap = {
    planner: 'planner',
    ceo: 'ceoReview',
    ceoreview: 'ceoReview',
    'ceo-review': 'ceoReview',
    eng: 'engReview',
    engreview: 'engReview',
    'eng-review': 'engReview',
    planlinus: 'planLinusReview',
    'plan-linus': 'planLinusReview',
    planlinusreview: 'planLinusReview',
    'plan-linus-review': 'planLinusReview',
    plancodex: 'planCodexReview',
    'plan-codex': 'planCodexReview',
    gatea: 'gateSafetyDone',
    'gate-a': 'gateSafetyDone',
    safety: 'gateSafetyDone',
    gateb: 'gateProjectFitDone',
    'gate-b': 'gateProjectFitDone',
    projectfit: 'gateProjectFitDone',
    gatec: 'gateTasteDone',
    'gate-c': 'gateTasteDone',
    taste: 'gateTasteDone',
    gated: 'gateCleanupDone',
    'gate-d': 'gateCleanupDone',
    cleanup: 'gateCleanupDone',
    gatex: 'gateXCodexDone',
    'gate-x': 'gateXCodexDone',
    codex: 'gateXCodexDone',
    build: 'buildPassed',
    buildpassed: 'buildPassed',
  };

  const actual = stepMap[stepToSkip];
  if (actual) {
    completeStep(actual);
    log('');
    log(`[Workflow] WARNING - SKIPPED: ${actual} (not recommended)`);
    log('[Workflow] For override with documented reason, use: workflow override <target> <reason ≥ 20 chars>');
    log('');
  } else {
    log('');
    log(`[Workflow] Unknown step: ${stepToSkip}`);
    log('[Workflow] Valid steps: planner, ceo, eng, plan-linus, plan-codex, gate-a..d..x (safety/projectfit/taste/cleanup/codex), build');
    log('');
  }
  process.exit(0);
}

// commit this - run full 5-gate pipeline sequentially then commit
if (/commit\s+this/i.test(promptLower)) {
  log('');
  log('<user-prompt-submit-hook>');
  log('COMMIT WORKFLOW TRIGGERED (v2 — 5-gate mission-based).');
  log('MANDATORY: Use Agent tool for each gate. Do NOT substitute with text summaries.');
  log('IMPORTANT: Code Phase gates are SEQUENTIAL. Each gate requires ledger disposition before next.');
  log('');
  log('Gate A — Safety (dispatch first):');
  log('  Agent tool -> subagent_type: "security-vuln-scanner"');
  log('  → Handle ledger-gateA-*.md MEDIUM+ findings (FIXED/DISMISSED/DEFERRED)');
  log('');
  log('Gate B — Project Fit (after Gate A disposition):');
  log('  Agent tool -> subagent_type: "code-review-specialist"');
  log('  → Handle ledger-gateB-*.md');
  log('');
  log('Gate C — Taste & Back-compat (after Gate B):');
  log('  Agent tool -> subagent_type: "linus-reviewer"');
  log('  → Handle ledger-gateC-*.md');
  log('');
  log('Gate D — Post-AI Cleanup (after Gate C):');
  log('  Agent tool -> subagent_type: "code-simplifier"');
  log('  → Handle ledger-gateD-*.md');
  log('');
  log('Gate X — Cross-Model Verification (after Gate D):');
  log('  Agent tool -> subagent_type: "gatex-codex-reviewer"');
  log('  → Handle ledger-gatex-codex-*.md (rerun gatex if verdict=needs-attention after fixes)');
  log('');
  log('Final: Build + stop-review-gate + commit (Conventional Commits, 繁體中文)');
  log('Override escape: workflow override <target> <reason ≥ 20 chars>');
  log('</user-prompt-submit-hook>');
  log('');
  process.exit(0);
}

// code-review - run full pipeline WITHOUT commit
if (/^code-review$/i.test(promptLower.trim())) {
  log('');
  log('<user-prompt-submit-hook>');
  log('CODE-REVIEW WORKFLOW TRIGGERED (v2 — 5-gate without commit).');
  log('MANDATORY: Use Agent tool for each gate.');
  log('');
  log('Sequence: Gate A → B → C → D → X, each gate requires ledger disposition before next.');
  log('  1. Agent: security-vuln-scanner        (Gate A Safety)');
  log('  2. Agent: code-review-specialist       (Gate B Project Fit)');
  log('  3. Agent: linus-reviewer               (Gate C Taste)');
  log('  4. Agent: code-simplifier              (Gate D Cleanup)');
  log('  5. Agent: gatex-codex-reviewer         (Gate X Cross-Model)');
  log('');
  log('Use "commit this" when ready to commit.');
  log('</user-prompt-submit-hook>');
  log('');
  process.exit(0);
}

// ============================================================================
// 3. Step completion text signals
// ============================================================================

if (/(planner\s+done|planning\s+done)/i.test(promptLower)) {
  completeStep('planner');
  log('');
  log('[Workflow] Planner step completed!');
  log('');
  process.exit(0);
}

// Agent-based steps are auto-completed by post-agent-verify.mjs.
// Text signals removed to prevent bypassing.

if (/(build\s+pass|build\s+success|build\s+done)/i.test(promptLower)) {
  completeStep('buildPassed');
  log('');
  log('[Workflow] Build step marked as passed!');
  log('');
  process.exit(0);
}

// ============================================================================
// 4. Requirement keywords → suggest planner
// ============================================================================

const plannerKeywordsEn = [
  'add feature', 'create feature', 'implement', 'new feature',
  'feature request', 'refactor', 'restructure',
  'architecture', 'design system', 'integrate', 'migration',
  'adjust', 'hope', 'add new', 'wish',
];

const plannerKeywordsCn = ['調整', '項目調整', '希望', '增加', '新增'];

let needsPlanner = plannerKeywordsEn.some((kw) => promptLower.includes(kw));
if (!needsPlanner) {
  needsPlanner = plannerKeywordsCn.some((kw) => prompt.includes(kw));
}

if (needsPlanner) {
  const state = getWorkflowState();
  if (!state.completedSteps.planner) {
    log('');
    log('=================================================');
    log(' PLANNING RECOMMENDED');
    log('=================================================');
    log('');
    log(' Detected keywords suggesting a complex task.');
    log(' Consider entering plan mode and dispatching all 4 plan reviewers in parallel:');
    log('   Agent: plan-ceo-reviewer');
    log('   Agent: plan-eng-reviewer');
    log('   Agent: plan-linus-reviewer');
    log('   Agent: plan-codex-adversarial-reviewer  (wrapper for cross-model review)');
    log('');
    log(" Or say 'planner done' to skip this step.");
    log('');
    log('=================================================');
    log('');
  }
}

// ============================================================================
// 5. Commit intent detection
// ============================================================================

const commitKeywords = ['commit', 'git commit', 'push'];
const hasCommitIntent = commitKeywords.some((kw) => promptLower.includes(kw));

if (hasCommitIntent) {
  const state = getWorkflowState();
  const codeFileExists = state.modifiedFiles.some((f) => isCodeFile(f));

  if (codeFileExists) {
    const codeFileCount = state.modifiedFiles.filter((f) => isCodeFile(f)).length;

    if (!allCodingGatesComplete()) {
      const missing = getCodingMissingGates();

      log('');
      log('=================================================');
      log(' WARNING: WORKFLOW STEPS REQUIRED BEFORE COMMIT');
      log('=================================================');
      log('');
      log(` Tracked files: ${state.modifiedFiles.length} (${codeFileCount} code)`);
      log('');
      log(' Missing gates:');
      for (const name of missing) {
        log(`   [ ] ${name}`);
      }

      log('');
      log(' Complete each gate + dispose ledger findings, then retry commit.');
      log('');
      log(' Commands:');
      log("   'workflow status'              View full status + ledger progress");
      log("   'workflow override <target> <reason ≥ 20>'  Emergency override");
      log('');
      log('=================================================');
      log('');
    } else {
      log('');
      log('=================================================');
      log(' ALL 5 CODING GATES COMPLETED!');
      log('=================================================');
      log('');
      log(' You may now create a commit.');
      log(' Final build + stop-review-gate will run automatically.');
      log('');
      log('=================================================');
      log('');
    }
  }
}

process.exit(0);
