// ============================================================================
// workflow-orchestrator.mjs - UserPromptSubmit Hook: Workflow Controller
// ============================================================================
// Detects user input and manages workflow state:
// - Detects step completion signals -> updates state
// - Detects commit intent -> shows pending checklist
// - Supports workflow status/reset/skip commands
// - Supports both Planning Phase and Coding Phase
// ============================================================================

import {
  getWorkflowState,
  resetWorkflowState,
  completeStep,
  getCodingMissingSteps,
  isCodeFile,
  showWorkflowStatus,
} from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

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
// 1. Handle workflow commands
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

// workflow skip <step>
const skipMatch = promptLower.match(/workflow\s+skip\s+(\w+)/);
if (skipMatch) {
  const stepToSkip = skipMatch[1];

  const stepMap = {
    planner: 'planner',
    codereview: 'codeReview',
    'code-review': 'codeReview',
    review: 'codeReview',
    linusreview: 'linusReview',
    'linus-review': 'linusReview',
    linus: 'linusReview',
    build: 'buildPassed',
    buildpassed: 'buildPassed',
    ceoreview: 'ceoReview',
    'ceo-review': 'ceoReview',
    ceo: 'ceoReview',
    engreview: 'engReview',
    'eng-review': 'engReview',
    eng: 'engReview',
    planlinusreview: 'planLinusReview',
    'plan-linus-review': 'planLinusReview',
    'plan-linus': 'planLinusReview',
    planlinus: 'planLinusReview',
  };

  const actual = stepMap[stepToSkip];
  if (actual) {
    completeStep(actual);
    log('');
    log(`[Workflow] WARNING - SKIPPED: ${actual} (not recommended)`);
    log('[Workflow] Please ensure quality is maintained!');
    log('');
  } else {
    log('');
    log(`[Workflow] Unknown step: ${stepToSkip}`);
    log('[Workflow] Valid steps: planner, codereview, linus, build, ceo, eng, plan-linus');
    log('');
  }
  process.exit(0);
}

// commit this - run full workflow sequentially then commit
if (/commit\s+this/i.test(promptLower)) {
  log('');
  log('<user-prompt-submit-hook>');
  log('COMMIT WORKFLOW TRIGGERED. Execute the following steps.');
  log('MANDATORY: Steps MUST use the Agent tool. Do NOT substitute with your own text summary.');
  log('');
  log('Step 1 — Combined Code Review (dispatch ALL THREE in parallel):');
  log('  Agent tool -> subagent_type: "code-simplifier:code-simplifier"');
  log('  Agent tool -> subagent_type: "code-review-specialist"');
  log('  Agent tool -> subagent_type: "security-vuln-scanner"');
  log('Step 2 — After all 3 complete, dispatch:');
  log('  Agent tool -> subagent_type: "linus-reviewer"');
  log('Step 3 — Build + commit:');
  log('  dotnet build backend/HansOS.slnx -> git commit (Conventional Commits, Traditional Chinese)');
  log('</user-prompt-submit-hook>');
  log('');
  process.exit(0);
}

// code-review - run full workflow WITHOUT commit
if (/^code-review$/i.test(promptLower.trim())) {
  log('');
  log('<user-prompt-submit-hook>');
  log('CODE-REVIEW WORKFLOW TRIGGERED. Execute the following steps.');
  log('MANDATORY: Steps MUST use the Agent tool. Do NOT substitute with your own text summary.');
  log('');
  log('Step 1 — Combined Code Review (dispatch ALL THREE in parallel):');
  log('  Agent tool -> subagent_type: "code-simplifier:code-simplifier"');
  log('  Agent tool -> subagent_type: "code-review-specialist"');
  log('  Agent tool -> subagent_type: "security-vuln-scanner"');
  log('Step 2 — After all 3 complete, dispatch:');
  log('  Agent tool -> subagent_type: "linus-reviewer"');
  log('Step 3 — Build verification (do NOT commit)');
  log('');
  log('Use "commit this" when ready to commit.');
  log('</user-prompt-submit-hook>');
  log('');
  process.exit(0);
}

// ============================================================================
// 2. Detect step completion signals
// ============================================================================

if (/(planner\s+done|planning\s+done)/i.test(promptLower)) {
  completeStep('planner');
  log('');
  log('[Workflow] Planner step completed!');
  log('');
  process.exit(0);
}

// Agent-based steps are auto-completed by post-agent-verify.mjs PostToolUse hook.
// Text signals removed to prevent bypassing.

if (/(build\s+pass|build\s+success|build\s+done)/i.test(promptLower)) {
  completeStep('buildPassed');
  log('');
  log('[Workflow] Build step marked as passed!');
  log('');
  process.exit(0);
}

// ============================================================================
// 3. Detect requirement keywords (suggest running planner)
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
    log(' Consider using the planning-architect agent first.');
    log('');
    log(" Or say 'planner done' to skip this step.");
    log('');
    log('=================================================');
    log('');
  }
}

// ============================================================================
// 4. Detect commit intent
// ============================================================================

const commitKeywords = ['commit', 'done', 'ready', 'git commit', 'push'];
const hasCommitIntent = commitKeywords.some((kw) => promptLower.includes(kw));

if (hasCommitIntent) {
  const state = getWorkflowState();
  const codeFileExists = state.modifiedFiles.some((f) => isCodeFile(f));

  if (codeFileExists) {
    const codeFileCount = state.modifiedFiles.filter((f) => isCodeFile(f)).length;
    const missing = getCodingMissingSteps();

    if (missing.length > 0) {
      const stepDisplayNames = {
        codeReview: 'Combined Code Review (run code-simplifier + code-review-specialist + security-vuln-scanner)',
        linusReview: 'Linus Review (run linus-reviewer agent)',
      };

      log('');
      log('=================================================');
      log(' WARNING: WORKFLOW STEPS REQUIRED BEFORE COMMIT');
      log('=================================================');
      log('');
      log(` Tracked files: ${state.modifiedFiles.length} (${codeFileCount} code)`);
      log('');
      log(' Missing steps:');

      for (const step of missing) {
        log(`   [ ] ${stepDisplayNames[step] || step}`);
      }

      log('');
      log(' Complete these steps, then commit will be allowed.');
      log('');
      log(' Commands:');
      log("   'workflow status' - View full status");
      log("   'workflow skip <step>' - Skip a step (not recommended)");
      log('');
      log('=================================================');
      log('');
    } else {
      log('');
      log('=================================================');
      log(' ALL WORKFLOW STEPS COMPLETED!');
      log('=================================================');
      log('');
      log(' You may now create a commit.');
      log(' Final build verification will run automatically.');
      log('');
      log('=================================================');
      log('');
    }
  }
}

process.exit(0);
