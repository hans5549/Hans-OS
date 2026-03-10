// ============================================================================
// workflow-state.mjs - Workflow State Management Module
// ============================================================================
// Provides state tracking for modified files and completed workflow steps
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
  '.cs', '.razor', '.json', '.css', '.js', '.html', '.csproj', '.xml',
  '.vue', '.ts', '.tsx', '.mts', '.jsx'
]);
const DOC_EXTENSIONS = new Set(['.md', '.txt', '.rst', '.yml', '.yaml']);

// ── Default state ──────────────────────────────────────────────────────────

function getDefaultState() {
  return {
    modifiedFiles: [],
    completedSteps: {
      planner: false,
      simplifier: false,
      specCheck: false,
      buildPassed: false,
      codeReviewer: false,
      securityReviewer: false,
      linusGreen: false,
    },
    buildRetryCount: 0,
    lastModified: '',
    sessionId: '',
    // === Cross-cutting state (used by global + project hooks) ===
    toolCallCount: 0,          // Area 1: Attention anchor frequency control
    currentPlanFile: '',        // Area 1: Active plan file path
    researchOpCount: 0,         // Area 2: Research operation counter
    editHistory: [],            // Area 3: Edit history for Strike tracking
    lastReadFiles: {},          // Area 8: File read timestamp tracking
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

    // Ensure cross-cutting fields exist (used by global hooks)
    if (state.toolCallCount === undefined) state.toolCallCount = 0;
    if (state.researchOpCount === undefined) state.researchOpCount = 0;
    if (!Array.isArray(state.editHistory)) state.editHistory = [];
    if (!state.lastReadFiles || typeof state.lastReadFiles !== 'object') state.lastReadFiles = {};

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

export function addModifiedFile(filePath) {
  const state = getWorkflowState();
  const normalized = filePath.replace(/\\/g, '/');

  // Exclude workflow infrastructure files to prevent self-triggering
  if (normalized.includes('.claude/')) {
    return state;
  }

  if (state.modifiedFiles.includes(normalized)) {
    return state;
  }

  state.modifiedFiles.push(normalized);

  // If code file, reset review steps (keep planner)
  if (isCodeFile(filePath)) {
    state.completedSteps.simplifier = false;
    state.completedSteps.specCheck = false;
    state.completedSteps.buildPassed = false;
    state.completedSteps.codeReviewer = false;
    state.completedSteps.securityReviewer = false;
    state.completedSteps.linusGreen = false;
  }

  setWorkflowState(state);
  return state;
}

// ── Step management ────────────────────────────────────────────────────────

const VALID_STEPS = ['planner', 'simplifier', 'specCheck', 'buildPassed', 'codeReviewer', 'securityReviewer', 'linusGreen'];

export function completeStep(stepName) {
  if (!VALID_STEPS.includes(stepName)) {
    throw new Error(`Invalid step: ${stepName}`);
  }
  const state = getWorkflowState();
  state.completedSteps[stepName] = true;
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

export function getMissingSteps(includePlanner = false) {
  const state = getWorkflowState();
  const required = ['simplifier', 'codeReviewer', 'securityReviewer'];

  if (includePlanner) {
    required.unshift('planner');
  }

  return required.filter((step) => !state.completedSteps[step]);
}

export function hasCodeFiles() {
  const state = getWorkflowState();
  return state.modifiedFiles.some((f) => isCodeFile(f));
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
  const stepOrder = [
    { key: 'planner', name: 'Planner (optional)' },
    { key: 'simplifier', name: 'Code Simplifier' },
    { key: 'specCheck', name: 'Spec Check' },
    { key: 'buildPassed', name: 'Build Passed' },
    { key: 'codeReviewer', name: 'Code Review' },
    { key: 'securityReviewer', name: 'Security Review' },
    { key: 'linusGreen', name: 'Linus Green' },
  ];

  for (const { key, name } of stepOrder) {
    const done = state.completedSteps[key];
    const icon = done ? '[x]' : '[ ]';
    log(`  ${icon} ${name}`);
  }

  log('');
  log('================================');
  log('');
}
