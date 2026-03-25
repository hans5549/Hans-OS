// ============================================================================
// workflow-state.mjs - Workflow State Management Module
// ============================================================================
// Provides state tracking for modified files and completed workflow steps.
// Supports both Planning Phase (CEO/Eng/Linus review) and Coding Phase.
// ============================================================================

import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';
import { extname, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const PROJECT_ROOT = resolve(__dirname, '..', '..');

const WORKFLOW_DIR = resolve(PROJECT_ROOT, '.claude', 'workflow');
const STATE_FILE = resolve(WORKFLOW_DIR, 'state.json');

const CODE_EXTENSIONS = new Set([
  '.cs', '.razor', '.json', '.css', '.js', '.html', '.csproj', '.xml', '.jsx', '.ts', '.tsx', '.vue', '.mts'
]);
const DOC_EXTENSIONS = new Set(['.md', '.txt', '.rst', '.yml', '.yaml']);

// ── Default state ──────────────────────────────────────────────────────────

function getDefaultState() {
  return {
    modifiedFiles: [],
    completedSteps: {
      // Planning phase
      planner: false,
      ceoReview: false,
      engReview: false,
      planLinusReview: false,
      // Coding phase
      simplifier: false,
      buildPassed: false,
      codeReviewer: false,
      securityReviewer: false,
    },
    buildRetryCount: 0,
    lastModified: '',
    sessionId: '',
    currentPlanFile: '',
    editHistory: [],
    // Track cumulative line changes since last review
    lineChangeSinceReview: 0,
    // Worktree merge-back flow
    mergeBackPending: false,
  };
}

// ── Directory & file helpers ───────────────────────────────────────────────

function ensureWorkflowDir() {
  if (!existsSync(WORKFLOW_DIR)) {
    mkdirSync(WORKFLOW_DIR, { recursive: true });
  }
}

// ── Read / Write state ─────────────────────────────────────────────────────

export function getWorkflowState() {
  ensureWorkflowDir();

  if (!existsSync(STATE_FILE)) {
    return getDefaultState();
  }

  try {
    const content = readFileSync(STATE_FILE, 'utf-8');
    const state = JSON.parse(content);
    const defaults = getDefaultState();

    // Ensure all required fields exist
    if (!Array.isArray(state.modifiedFiles)) {
      state.modifiedFiles = [];
    }

    if (!state.completedSteps || typeof state.completedSteps !== 'object') {
      state.completedSteps = defaults.completedSteps;
    } else {
      for (const step of Object.keys(defaults.completedSteps)) {
        if (state.completedSteps[step] === undefined) {
          state.completedSteps[step] = false;
        }
      }
    }

    // Ensure other fields exist
    if (!Array.isArray(state.editHistory)) state.editHistory = [];
    if (state.currentPlanFile === undefined) state.currentPlanFile = '';
    if (state.lineChangeSinceReview === undefined) state.lineChangeSinceReview = 0;
    if (state.mergeBackPending === undefined) state.mergeBackPending = false;

    return state;
  } catch {
    process.stderr.write('Warning: Failed to read state file, using default\n');
    return getDefaultState();
  }
}

export function setWorkflowState(state) {
  ensureWorkflowDir();
  state.lastModified = new Date().toISOString();

  try {
    writeFileSync(STATE_FILE, JSON.stringify(state, null, 2), 'utf-8');
    return true;
  } catch (err) {
    process.stderr.write(`Error: Failed to write state file: ${err.message}\n`);
    return false;
  }
}

// ── File type checks ───────────────────────────────────────────────────────

export function isCodeFile(filePath) {
  return CODE_EXTENSIONS.has(extname(filePath).toLowerCase());
}

export function isDocFile(filePath) {
  return DOC_EXTENSIONS.has(extname(filePath).toLowerCase());
}

// ── File tracking ──────────────────────────────────────────────────────────

export function addModifiedFile(filePath, lineCount = 0) {
  const state = getWorkflowState();
  const normalized = filePath.replace(/\\/g, '/');

  // Exclude workflow infrastructure files to prevent self-triggering
  if (normalized.includes('.claude/')) {
    return state;
  }

  const isNew = !state.modifiedFiles.includes(normalized);
  if (isNew) {
    state.modifiedFiles.push(normalized);
  }

  // If code file, handle review step resets
  if (isCodeFile(filePath)) {
    state.lineChangeSinceReview = (state.lineChangeSinceReview || 0) + lineCount;

    if (state.completedSteps.simplifier) {
      // Simplifier already done → only reset post-simplifier steps
      // Small changes (< 10 lines cumulative) → warn but don't reset
      if (state.lineChangeSinceReview >= 10) {
        state.completedSteps.codeReviewer = false;
        state.completedSteps.securityReviewer = false;
        state.completedSteps.buildPassed = false;
        state.lineChangeSinceReview = 0;
      }
    } else {
      // Simplifier not done → reset all coding review steps
      state.completedSteps.simplifier = false;
      state.completedSteps.buildPassed = false;
      state.completedSteps.codeReviewer = false;
      state.completedSteps.securityReviewer = false;
    }
  }

  setWorkflowState(state);
  return state;
}

// ── Step management ────────────────────────────────────────────────────────

const VALID_STEPS = [
  'planner', 'ceoReview', 'engReview', 'planLinusReview',
  'simplifier', 'buildPassed', 'codeReviewer', 'securityReviewer',
];

export function completeStep(stepName) {
  if (!VALID_STEPS.includes(stepName)) {
    throw new Error(`Invalid step: ${stepName}`);
  }
  const state = getWorkflowState();
  state.completedSteps[stepName] = true;
  // Reset cumulative line counter when review completes
  if (['codeReviewer', 'securityReviewer'].includes(stepName)) {
    state.lineChangeSinceReview = 0;
  }
  setWorkflowState(state);
  return state;
}

export function resetStep(stepName) {
  if (!VALID_STEPS.includes(stepName)) {
    throw new Error(`Invalid step: ${stepName}`);
  }
  const state = getWorkflowState();
  state.completedSteps[stepName] = false;
  setWorkflowState(state);
  return state;
}

export function resetWorkflowState() {
  const state = getDefaultState();
  setWorkflowState(state);
  return state;
}

// ── Query helpers ──────────────────────────────────────────────────────────

export function getCodingMissingSteps() {
  const state = getWorkflowState();
  const required = ['simplifier', 'codeReviewer', 'securityReviewer'];
  return required.filter((step) => !state.completedSteps[step]);
}

export function getPlanningMissingSteps() {
  const state = getWorkflowState();
  const required = ['ceoReview', 'engReview', 'planLinusReview'];
  return required.filter((step) => !state.completedSteps[step]);
}

export function hasCodeFiles() {
  const state = getWorkflowState();
  return state.modifiedFiles.some((f) => isCodeFile(f));
}

// ── Merge-back helpers ───────────────────────────────────────────────

export function resetCodingStepsOnly() {
  const state = getWorkflowState();
  state.completedSteps.simplifier = false;
  state.completedSteps.codeReviewer = false;
  state.completedSteps.securityReviewer = false;
  state.completedSteps.buildPassed = false;
  state.lineChangeSinceReview = 0;
  setWorkflowState(state);
  return state;
}

// ── Display ────────────────────────────────────────────────────────────────

export function showWorkflowStatus() {
  const state = getWorkflowState();
  const log = (msg) => process.stderr.write(msg + '\n');

  log('');
  log('====== Workflow Status ======');
  log('');

  // Modified files
  log('Modified Files:');
  if (state.modifiedFiles.length === 0) {
    log('  (none)');
  } else {
    for (const file of state.modifiedFiles) {
      const icon = isCodeFile(file) ? '[CODE]' : '[DOC]';
      log(`  ${icon} ${file}`);
    }
  }
  log('');

  // Step status
  log('Completed Steps:');

  log('  -- Planning Phase --');
  const planSteps = [
    { key: 'planner', name: 'Planner (optional)' },
    { key: 'ceoReview', name: 'CEO Review' },
    { key: 'engReview', name: 'Eng Review' },
    { key: 'planLinusReview', name: 'Plan Linus Review' },
  ];
  for (const { key, name } of planSteps) {
    const done = state.completedSteps[key];
    const icon = done ? '[x]' : '[ ]';
    log(`  ${icon} ${name}`);
  }

  log('  -- Coding Phase --');
  const codeSteps = [
    { key: 'simplifier', name: 'Code Simplifier' },
    { key: 'buildPassed', name: 'Build Passed' },
    { key: 'codeReviewer', name: 'Code Review' },
    { key: 'securityReviewer', name: 'Security Review' },
  ];
  for (const { key, name } of codeSteps) {
    const done = state.completedSteps[key];
    const icon = done ? '[x]' : '[ ]';
    log(`  ${icon} ${name}`);
  }

  if (state.mergeBackPending) {
    log('');
    log('  -- Merge-Back --');
    log('  [!] Merge-back pending — run "worktree merge-back" after reviews pass');
  }

  if (state.currentPlanFile) {
    log('');
    log(`  Active Plan: ${state.currentPlanFile}`);
  }

  log('');
  log('================================');
  log('');
}
