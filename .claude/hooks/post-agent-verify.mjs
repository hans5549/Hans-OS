// ============================================================================
// post-agent-verify.mjs - PostToolUse Hook: Agent Execution Verifier
// ============================================================================
// Only marks workflow steps complete when the actual Agent tool is called.
// This prevents bypassing workflow by simply typing "simplifier done" etc.
// Supports both Planning Phase and Coding Phase agents.
// ============================================================================

import { completeStep } from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

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
  // Planning phase agents
  { pattern: /plan-ceo-review/i, step: 'ceoReview', name: 'CEO Review' },
  { pattern: /plan-eng-review/i, step: 'engReview', name: 'Eng Review' },
  { pattern: /plan-linus-review/i, step: 'planLinusReview', name: 'Plan Linus Review' },
  // Coding phase agents
  { pattern: /code-simplifier/i, step: 'simplifier', name: 'Code Simplifier' },
  { pattern: /code-review-specialist/i, step: 'codeReviewer', name: 'Code Review' },
  { pattern: /security-vuln-scanner/i, step: 'securityReviewer', name: 'Security Review' },
];

for (const { pattern, step, name } of AGENT_STEP_MAP) {
  if (pattern.test(searchText)) {
    completeStep(step);
    log('[Workflow] ' + name + ' agent executed \u2014 step auto-completed');
    break;
  }
}

process.exit(0);
