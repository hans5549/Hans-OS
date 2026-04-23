// ============================================================================
// workflow-state.mjs - Workflow State Management Module (v2 schema)
// ============================================================================
// Provides state tracking for modified files and completed workflow steps.
// Supports Plan Phase (4 reviewers) and Code Phase (5 gates).
//
// UPDATED (pipeline redesign):
// - schemaVersion: 2
// - New completedSteps schema with gate{A,B,C,D} + gateX + plan codex
// - v1 → v2 auto-migration on read
// - overrides{} tracks workflow override <target> <reason> state
// - Legacy 6 fields removed (simplifier / specCheck / codeReviewer / ...)
// - Gate completion judgment moved to workflow-gates.mjs
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

const SMART_RESET_THRESHOLD = 10;

// ── Default state (v2 schema) ──────────────────────────────────────────────

function getDefaultState() {
  return {
    schemaVersion: 2,
    modifiedFiles: [],
    completedSteps: {
      // Planning phase
      planner: false,
      ceoReview: false,
      engReview: false,
      planLinusReview: false,
      planCodexReview: false,
      planCodexVerdict: null,        // "approve" | "needs-attention" | "degraded" | null
      // Coding phase (5 gates)
      gateSafetyDone: false,
      gateProjectFitDone: false,
      gateTasteDone: false,
      gateCleanupDone: false,
      gateXCodexDone: false,
      gateXCodexVerdict: null,       // "approve" | "needs-attention" | "degraded" | null
      buildPassed: false,
    },
    overrides: {},                    // { gateX: {reason, timestamp}, "unblock-next": {active, reason}, ... }
    buildRetryCount: 0,
    lastModified: '',
    sessionId: '',
    currentPlanFile: '',
    currentTask: '',
    lineChangeSinceReview: 0,
  };
}

// ── Directory & file helpers ───────────────────────────────────────────────

function ensureWorkflowDir() {
  if (!existsSync(WORKFLOW_DIR)) {
    mkdirSync(WORKFLOW_DIR, { recursive: true });
  }
}

// ── v1 → v2 migration ──────────────────────────────────────────────────────

function migrateV1ToV2(state) {
  const defaults = getDefaultState();
  state.completedSteps ??= {};

  // Map old booleans to new gate flags
  if (state.completedSteps.codeReview === true) {
    state.completedSteps.gateSafetyDone = true;
    state.completedSteps.gateProjectFitDone = true;
    state.completedSteps.gateCleanupDone = true;
  }
  if (state.completedSteps.linusReview === true) {
    state.completedSteps.gateTasteDone = true;
  }

  // Remove legacy keys
  const legacyKeys = [
    'codeReview', 'linusReview',
    'simplifier', 'specCheck', 'codeReviewer',
    'securityReviewer', 'linusGreen', 'mergeBackPending',
  ];
  for (const key of legacyKeys) {
    delete state.completedSteps[key];
    delete state[key];
  }

  // Fill defaults for all new v2 fields
  for (const [key, val] of Object.entries(defaults.completedSteps)) {
    if (state.completedSteps[key] === undefined) {
      state.completedSteps[key] = val;
    }
  }
  state.overrides ??= {};
  state.currentTask ??= '';
  state.schemaVersion = 2;

  return state;
}

// ── Read / Write state ─────────────────────────────────────────────────────

export function getWorkflowState() {
  ensureWorkflowDir();

  if (!existsSync(STATE_FILE)) {
    return getDefaultState();
  }

  try {
    const content = readFileSync(STATE_FILE, 'utf-8');
    let state = JSON.parse(content);

    // v1 → v2 migration
    if (!state.schemaVersion || state.schemaVersion < 2) {
      state = migrateV1ToV2(state);
      setWorkflowState(state);
    }

    // Defensive: ensure required fields exist even after migration
    const defaults = getDefaultState();
    if (!Array.isArray(state.modifiedFiles)) state.modifiedFiles = [];
    if (!state.completedSteps || typeof state.completedSteps !== 'object') {
      state.completedSteps = defaults.completedSteps;
    }
    for (const step of Object.keys(defaults.completedSteps)) {
      if (state.completedSteps[step] === undefined) {
        state.completedSteps[step] = defaults.completedSteps[step];
      }
    }
    if (state.currentPlanFile === undefined) state.currentPlanFile = '';
    if (state.currentTask === undefined) state.currentTask = '';
    if (state.lineChangeSinceReview === undefined) state.lineChangeSinceReview = 0;
    if (!state.overrides || typeof state.overrides !== 'object') state.overrides = {};

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

// ── File tracking (with gate-aware smart reset) ────────────────────────────

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

  if (isCodeFile(filePath)) {
    state.lineChangeSinceReview = (state.lineChangeSinceReview || 0) + lineCount;

    // Smart reset: if any gate is complete, apply threshold
    const anyGateDone = state.completedSteps.gateSafetyDone ||
                       state.completedSteps.gateProjectFitDone ||
                       state.completedSteps.gateTasteDone ||
                       state.completedSteps.gateCleanupDone;

    if (anyGateDone) {
      if (state.lineChangeSinceReview >= SMART_RESET_THRESHOLD) {
        // Reset all coding phase gates + build
        state.completedSteps.gateSafetyDone = false;
        state.completedSteps.gateProjectFitDone = false;
        state.completedSteps.gateTasteDone = false;
        state.completedSteps.gateCleanupDone = false;
        state.completedSteps.gateXCodexDone = false;
        state.completedSteps.gateXCodexVerdict = null;
        state.completedSteps.buildPassed = false;
        state.lineChangeSinceReview = 0;
      }
    } else {
      // No gate done yet → reset ensures fresh start
      state.completedSteps.gateSafetyDone = false;
      state.completedSteps.gateProjectFitDone = false;
      state.completedSteps.gateTasteDone = false;
      state.completedSteps.gateCleanupDone = false;
      state.completedSteps.gateXCodexDone = false;
      state.completedSteps.buildPassed = false;
    }
  }

  setWorkflowState(state);
  return state;
}

// ── Step management ────────────────────────────────────────────────────────

const VALID_STEPS = [
  // Planning
  'planner', 'ceoReview', 'engReview', 'planLinusReview', 'planCodexReview',
  // Coding (5 gates)
  'gateSafetyDone', 'gateProjectFitDone', 'gateTasteDone',
  'gateCleanupDone', 'gateXCodexDone',
  'buildPassed',
];

export function completeStep(stepName) {
  if (!VALID_STEPS.includes(stepName)) {
    throw new Error(`Invalid step: ${stepName}`);
  }
  const state = getWorkflowState();
  state.completedSteps[stepName] = true;
  // Reset cumulative line counter when Gate A (first gate) completes
  if (stepName === 'gateSafetyDone') {
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

// ── Query helpers (kept for backward compat; prefer workflow-gates.mjs) ────

export function getPlanningMissingSteps() {
  const state = getWorkflowState();
  const required = [
    ['ceoReview', 'CEO Review'],
    ['engReview', 'Eng Review'],
    ['planLinusReview', 'Plan Linus Review'],
    ['planCodexReview', 'Plan Codex Adversarial Review'],
  ];
  return required.filter(([key]) => !state.completedSteps[key]).map(([, name]) => name);
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
  log('====== Workflow Status (v2 schema) ======');
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

  log('  -- Planning Phase (4 reviewers) --');
  const planSteps = [
    { key: 'planner', name: 'Planner (optional)' },
    { key: 'ceoReview', name: 'CEO Review' },
    { key: 'engReview', name: 'Eng Review' },
    { key: 'planLinusReview', name: 'Plan Linus Review' },
    { key: 'planCodexReview', name: 'Plan Codex Adversarial Review' },
  ];
  for (const { key, name } of planSteps) {
    const done = state.completedSteps[key];
    const icon = done ? '[x]' : '[ ]';
    log(`  ${icon} ${name}`);
  }
  if (state.completedSteps.planCodexVerdict) {
    log(`       planCodexVerdict: ${state.completedSteps.planCodexVerdict}`);
  }

  log('  -- Coding Phase (5 gates) --');
  const codeSteps = [
    { key: 'gateSafetyDone', name: 'Gate A — Safety' },
    { key: 'gateProjectFitDone', name: 'Gate B — Project Fit' },
    { key: 'gateTasteDone', name: 'Gate C — Taste' },
    { key: 'gateCleanupDone', name: 'Gate D — Cleanup' },
    { key: 'gateXCodexDone', name: 'Gate X — Cross-Model' },
    { key: 'buildPassed', name: 'Build Passed' },
  ];
  for (const { key, name } of codeSteps) {
    const done = state.completedSteps[key];
    const icon = done ? '[x]' : '[ ]';
    log(`  ${icon} ${name}`);
  }
  if (state.completedSteps.gateXCodexVerdict) {
    log(`       gateXCodexVerdict: ${state.completedSteps.gateXCodexVerdict}`);
  }

  // Active overrides
  const overrides = Object.entries(state.overrides || {}).filter(([, v]) => {
    if (!v) return false;
    if (typeof v === 'object' && v.active === false) return false;
    return true;
  });
  if (overrides.length > 0) {
    log('');
    log('  -- Active Overrides --');
    for (const [target, data] of overrides) {
      const reason = typeof data === 'object' ? data.reason : data;
      log(`  ! ${target}: ${String(reason).slice(0, 80)}${String(reason).length > 80 ? '...' : ''}`);
    }
  }

  if (state.currentPlanFile) {
    log('');
    log(`  Active Plan: ${state.currentPlanFile}`);
  }
  if (state.currentTask) {
    log(`  Active Task: ${state.currentTask}`);
  }

  log('');
  log('=========================================');
  log('');
}
