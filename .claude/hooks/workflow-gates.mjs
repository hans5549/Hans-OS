// ============================================================================
// workflow-gates.mjs - Gate completion judgment API
// ============================================================================
// Extracted from workflow-state.mjs to prevent single-file bloat.
// Reads state via workflow-state and applies gate-specific completion logic.
// ============================================================================

import { getWorkflowState } from './workflow-state.mjs';

// ── Code phase gate completion ──────────────────────────────────────────────

export function isGateSafetyComplete() {
  const state = getWorkflowState();
  return Boolean(state.completedSteps?.gateSafetyDone);
}

export function isGateProjectFitComplete() {
  const state = getWorkflowState();
  return Boolean(state.completedSteps?.gateProjectFitDone);
}

export function isGateTasteComplete() {
  const state = getWorkflowState();
  return Boolean(state.completedSteps?.gateTasteDone);
}

export function isGateCleanupComplete() {
  const state = getWorkflowState();
  return Boolean(state.completedSteps?.gateCleanupDone);
}

export function isGateXComplete() {
  const state = getWorkflowState();
  const steps = state.completedSteps || {};
  if (!steps.gateXCodexDone) return false;
  const overridden = Boolean(state.overrides?.gateX);
  const verdict = steps.gateXCodexVerdict;
  return verdict === 'approve' || overridden;
}

// ── Plan phase completion ───────────────────────────────────────────────────

export function isPlanPhaseComplete() {
  const state = getWorkflowState();
  const steps = state.completedSteps || {};
  const allReviewers = Boolean(
    steps.ceoReview &&
    steps.engReview &&
    steps.planLinusReview &&
    steps.planCodexReview
  );
  if (!allReviewers) return false;
  const verdict = steps.planCodexVerdict;
  const overridden = Boolean(state.overrides?.planCodex);
  return verdict !== 'needs-attention' || overridden;
}

// ── Aggregate: all coding gates done ────────────────────────────────────────

export function allCodingGatesComplete() {
  return (
    isGateSafetyComplete() &&
    isGateProjectFitComplete() &&
    isGateTasteComplete() &&
    isGateCleanupComplete() &&
    isGateXComplete()
  );
}

export function getCodingMissingGates() {
  const checks = [
    [isGateSafetyComplete, 'Gate A (Safety)'],
    [isGateProjectFitComplete, 'Gate B (Project Fit)'],
    [isGateTasteComplete, 'Gate C (Taste)'],
    [isGateCleanupComplete, 'Gate D (Cleanup)'],
    [isGateXComplete, 'Gate X (Cross-Model)'],
  ];
  return checks.filter(([fn]) => !fn()).map(([, name]) => name);
}

export function getPlanningMissingSteps() {
  const state = getWorkflowState();
  const steps = state.completedSteps || {};
  const required = [
    ['ceoReview', 'CEO Review'],
    ['engReview', 'Eng Review'],
    ['planLinusReview', 'Plan Linus Review'],
    ['planCodexReview', 'Plan Codex Adversarial Review'],
  ];
  return required.filter(([key]) => !steps[key]).map(([, name]) => name);
}
