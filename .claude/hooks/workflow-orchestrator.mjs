// ============================================================================
// workflow-orchestrator.mjs - UserPromptSubmit Hook: Workflow Controller
// ============================================================================
// Detects user input and manages workflow state:
// - Detects requirement keywords -> suggests running planner
// - Detects step completion signals -> updates state
// - Detects commit intent -> shows pending checklist
// - Supports workflow status/reset/skip commands
// ============================================================================

import {
  getWorkflowState,
  resetWorkflowState,
  completeStep,
  getMissingSteps,
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
  // parseHookInput already logged the error
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
    simplifier: 'simplifier',
    build: 'buildPassed',
    buildpassed: 'buildPassed',
    codereview: 'codeReviewer',
    codereviewer: 'codeReviewer',
    'code-review': 'codeReviewer',
    securityreview: 'securityReviewer',
    securityreviewer: 'securityReviewer',
    'security-review': 'securityReviewer',
    security: 'securityReviewer',
    speccheck: 'specCheck',
    'spec-check': 'specCheck',
    spec: 'specCheck',
    linus: 'linusGreen',
    linusgreen: 'linusGreen',
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
    log('[Workflow] Valid steps: planner, simplifier, speccheck, build, codereview, securityreview, linus');
    log('');
  }
  process.exit(0);
}

// commit this - run full workflow sequentially then commit
if (/commit\s+this/i.test(promptLower)) {
  log('');
  log('<user-prompt-submit-hook>');
  log('COMMIT WORKFLOW TRIGGERED. Execute the following steps SEQUENTIALLY.');
  log('MANDATORY: Steps 1, 2, 3 MUST use the Agent tool. Do NOT substitute with your own text summary.');
  log('');
  log('Step 1: Agent tool -> subagent_type: "code-simplifier:code-simplifier"');
  log('Step 2: Agent tool -> subagent_type: "code-review-specialist"');
  log('Step 3: Agent tool -> subagent_type: "security-vuln-scanner"');
  log('Step 4: dotnet build backend/HansOS.slnx -> git commit (Conventional Commits, Traditional Chinese)');
  log('');
  log('For detailed instructions, use /commit-this command instead.');
  log('</user-prompt-submit-hook>');
  log('');
  process.exit(0);
}

// code-review - run full workflow WITHOUT commit
if (/^code-review$/i.test(promptLower.trim())) {
  log('');
  log('<user-prompt-submit-hook>');
  log('CODE-REVIEW WORKFLOW TRIGGERED. Execute the following steps SEQUENTIALLY.');
  log('MANDATORY: Steps 1, 2, 3 MUST use the Agent tool. Do NOT substitute with your own text summary.');
  log('');
  log('Step 1: Agent tool -> subagent_type: "code-simplifier:code-simplifier"');
  log('Step 2: Agent tool -> subagent_type: "code-review-specialist"');
  log('Step 3: Agent tool -> subagent_type: "security-vuln-scanner"');
  log('Step 4: dotnet build backend/HansOS.slnx -> report result (do NOT commit)');
  log('');
  log('For detailed instructions, use /review-workflow command instead.');
  log('Use "commit this" or /commit-this when ready to commit.');
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

// Agent-based steps (simplifier, code review, security review) are now
// auto-completed by post-agent-verify.mjs PostToolUse hook.
// Text signals removed to prevent bypassing.

if (/(spec\s*check\s+done|spec\s+done)/i.test(promptLower)) {
  completeStep('specCheck');
  log('');
  log('[Workflow] Spec Check step completed!');
  log('');
  process.exit(0);
}

if (/(linus\s+green|linus\s+pass)/i.test(promptLower)) {
  completeStep('linusGreen');
  log('');
  log('[Workflow] Linus Review passed (Green)!');
  log('');
  process.exit(0);
}

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
    const missing = getMissingSteps();

    if (missing.length > 0) {
      const stepDisplayNames = {
        simplifier: 'Code Simplifier (run code-simplifier:code-simplifier agent)',
        codeReviewer: 'Code Review (run code-review-specialist agent)',
        securityReviewer: 'Security Review (run security-vuln-scanner agent)',
      };

      log('');
      log('=================================================');
      log(' WARNING: WORKFLOW STEPS REQUIRED BEFORE COMMIT');
      log('=================================================');
      log('');
      log(` Tracked files: ${state.modifiedFiles.length}`);
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
