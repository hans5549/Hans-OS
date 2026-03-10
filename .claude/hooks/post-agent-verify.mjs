// ============================================================================
// post-agent-verify.mjs - PostToolUse Hook: Agent Execution Verifier
// ============================================================================
// Only marks workflow steps complete when the actual Agent tool is called.
// This prevents bypassing workflow by simply typing "simplifier done" etc.
// ============================================================================

import { completeStep } from './workflow-state.mjs';
import { parseHookInput, log } from './hook-utils.mjs';

const parsed = await parseHookInput();
if (!parsed) process.exit(0);

const subagentType = (parsed.tool_input && parsed.tool_input.subagent_type) || '';

// Agent name -> workflow step mapping
const AGENT_STEP_MAP = [
  { pattern: /code-simplifier/i, step: 'simplifier', name: 'Code Simplifier' },
  { pattern: /code-review-specialist/i, step: 'codeReviewer', name: 'Code Review' },
  { pattern: /security-vuln-scanner/i, step: 'securityReviewer', name: 'Security Review' },
];

for (const { pattern, step, name } of AGENT_STEP_MAP) {
  if (pattern.test(subagentType)) {
    completeStep(step);
    log(`[Workflow] ${name} agent executed — step auto-completed`);
    break;
  }
}

process.exit(0);
