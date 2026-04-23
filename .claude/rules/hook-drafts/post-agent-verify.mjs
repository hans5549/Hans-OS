// ============================================================================
// post-agent-verify.mjs - PostToolUse Hook: Agent Execution Verifier + Ledger Generator
// ============================================================================
// Fires when the Agent tool completes. Does two things:
// 1. Marks workflow step complete based on subagent_type pattern
// 2. Parses Machine-Readable Findings from agent output → builds ledger file
//
// This prevents bypassing workflow by simply typing "simplifier done" etc.
// Supports Plan phase (4 reviewers) and Code phase (5 gates).
//
// UPDATED (pipeline redesign):
// - AGENT_STEP_MAP covers 9 reviewers (Plan 4 + Code 4 + Gate X wrapper)
// - After marking step, generates ledger via ledger-manager helper
// ============================================================================

import { completeStep, getWorkflowState } from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';
import {
  parseStructuredFindings,
  buildLedger,
  writeLedger,
  buildLedgerPath,
  timestampNow,
} from './ledger-manager.mjs';

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const subagentType = (parsed.tool_input && parsed.tool_input.subagent_type) || '';
const prompt = (parsed.tool_input && parsed.tool_input.prompt) || '';
const description = (parsed.tool_input && parsed.tool_input.description) || '';
const searchText = [subagentType, prompt, description].join(' ');

// Agent name -> workflow step mapping
// Matches against subagent_type, prompt, AND description to support
// custom agents dispatched via general-purpose with descriptive prompts
const AGENT_STEP_MAP = [
  // Plan phase (4 reviewers, all Agent tool)
  { pattern: /plan-ceo-review/i, step: 'ceoReview', name: 'CEO Review' },
  { pattern: /plan-eng-review/i, step: 'engReview', name: 'Eng Review' },
  { pattern: /plan-linus-review/i, step: 'planLinusReview', name: 'Plan Linus Review' },
  { pattern: /plan-codex-adversarial-reviewer/i, step: 'planCodexReview', name: 'Plan Codex Adversarial Review' },
  // Code phase Gate A-D (Claude agents)
  { pattern: /security-vuln-scanner/i, step: 'gateSafetyDone', name: 'Gate A Safety' },
  { pattern: /code-review-specialist/i, step: 'gateProjectFitDone', name: 'Gate B Project Fit' },
  { pattern: /linus-reviewer/i, step: 'gateTasteDone', name: 'Gate C Taste' },
  { pattern: /code-simplifier/i, step: 'gateCleanupDone', name: 'Gate D Cleanup' },
  // Code phase Gate X (Codex wrapper subagent, unified mode)
  { pattern: /gatex-codex-reviewer/i, step: 'gateXCodexDone', name: 'Gate X Cross-Model' },
];

let matchedEntry = null;
for (const entry of AGENT_STEP_MAP) {
  if (entry.pattern.test(searchText)) {
    try {
      completeStep(entry.step);
      log(`[Workflow] ${entry.name} agent executed — step auto-completed`);
    } catch (err) {
      log(`[Workflow] WARNING: completeStep(${entry.step}) failed: ${err.message}`);
    }
    matchedEntry = entry;
    break;
  }
}

// ── Build Ledger from structured findings ───────────────────────────────────

if (matchedEntry) {
  try {
    const toolResponse = parsed.tool_response || {};
    const agentOutput =
      toolResponse.content ||
      toolResponse.text ||
      (typeof toolResponse === 'string' ? toolResponse : '');

    if (agentOutput && /Machine-Readable Findings/i.test(agentOutput)) {
      const findings = parseStructuredFindings(agentOutput);

      // Determine phase, gate, and short agent name for ledger path
      let phase, gate, shortName;
      const st = matchedEntry.pattern.source.toLowerCase();

      if (/plan-/i.test(subagentType) || /plan/i.test(st)) {
        phase = 'plan';
        gate = 'plan';
        if (/ceo/i.test(subagentType)) shortName = 'ceo';
        else if (/eng/i.test(subagentType)) shortName = 'eng';
        else if (/plan-linus/i.test(subagentType)) shortName = 'linus';
        else if (/codex/i.test(subagentType)) shortName = 'codex';
        else shortName = 'unknown';
      } else {
        phase = 'code';
        if (/security/i.test(subagentType))      { gate = 'gateA'; shortName = 'security'; }
        else if (/specialist/i.test(subagentType)) { gate = 'gateB'; shortName = 'specialist'; }
        else if (/linus/i.test(subagentType))     { gate = 'gateC'; shortName = 'linus'; }
        else if (/simplifier/i.test(subagentType)) { gate = 'gateD'; shortName = 'simplifier'; }
        else if (/gatex/i.test(subagentType))     { gate = 'gatex'; shortName = 'codex'; }
        else { gate = 'unknown'; shortName = 'unknown'; }
      }

      if (gate !== 'unknown') {
        const ts = timestampNow();
        const path = buildLedgerPath(phase, gate, shortName, ts);
        const state = getWorkflowState();
        const content = buildLedger({
          gate,
          agent: shortName,
          task: state.currentTask || state.currentPlanFile || '(unknown)',
          findings,
        });
        writeLedger(path, content);
        log(`[Workflow] Ledger created: ${path.replace(/^.*\.claude/, '.claude')}`);
        log(`[Workflow]   ${findings.length} findings parsed (LOW/INFO auto-dismissed, MEDIUM+ need disposition)`);
      }
    }
  } catch (err) {
    // Ledger generation is non-critical. Log and continue.
    log(`[Workflow] WARNING: ledger generation failed: ${err.message}`);
  }
}

process.exit(0);
