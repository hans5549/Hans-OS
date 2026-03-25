#!/usr/bin/env bash
# ============================================================================
# workflow-orchestrator.sh - userPromptSubmitted Hook: Workflow Controller
# ============================================================================
# Ported from: .claude/hooks/workflow-orchestrator.mjs
# Detects workflow commands, step completion signals, and commit intent.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Read stdin (JSON from Copilot CLI) ─────────────────────────────────────

INPUT=$(cat)
PROMPT=$(echo "$INPUT" | jq -r '.userPrompt // .prompt // ""' 2>/dev/null) || exit 0
[[ -z "$PROMPT" ]] && exit 0

PROMPT_LOWER=$(echo "$PROMPT" | tr '[:upper:]' '[:lower:]')

# ============================================================================
# 1. Handle workflow commands
# ============================================================================

# workflow status
if echo "$PROMPT_LOWER" | grep -qE 'workflow\s+status'; then
  show_workflow_status
  exit 0
fi

# workflow reset
if echo "$PROMPT_LOWER" | grep -qE 'workflow\s+reset'; then
  reset_workflow_state
  log ""
  log "[Workflow] State has been reset!"
  log ""
  exit 0
fi

# workflow skip <step>
if echo "$PROMPT_LOWER" | grep -qE 'workflow\s+skip\s+\w+'; then
  step_to_skip=$(echo "$PROMPT_LOWER" | sed -n 's/.*workflow\s\+skip\s\+\(\w\+\).*/\1/p')

  case "$step_to_skip" in
    planner) actual="planner" ;;
    simplifier) actual="simplifier" ;;
    build|buildpassed) actual="buildPassed" ;;
    codereview|codereviewer|code-review) actual="codeReviewer" ;;
    securityreview|securityreviewer|security-review|security) actual="securityReviewer" ;;
    ceoreview|ceo-review|ceo) actual="ceoReview" ;;
    engreview|eng-review|eng) actual="engReview" ;;
    planlinusreview|plan-linus-review|plan-linus|planlinus) actual="planLinusReview" ;;
    *) actual="" ;;
  esac

  if [[ -n "$actual" ]]; then
    complete_step "$actual"
    log ""
    log "[Workflow] WARNING - SKIPPED: $actual (not recommended)"
    log "[Workflow] Please ensure quality is maintained!"
    log ""
  else
    log ""
    log "[Workflow] Unknown step: $step_to_skip"
    log "[Workflow] Valid steps: planner, simplifier, build, codereview, security, ceo, eng, plan-linus"
    log ""
  fi
  exit 0
fi

# commit this - trigger full workflow
if echo "$PROMPT_LOWER" | grep -qE 'commit\s+this'; then
  log ""
  log "<user-prompt-submit-hook>"
  log "COMMIT WORKFLOW TRIGGERED. Execute @commit-workflow agent."
  log ""
  log "Steps:"
  log "  1. @code-simplifier → simplify code"
  log "  2. @code-review → code quality review"
  log "  3. @security-scanner → security scan"
  log "  4. @linus-reviewer → Good Taste review"
  log "  5. @gstack-reviewer → structural review"
  log "  6. Build verification + git commit"
  log ""
  log "Or invoke: @commit-workflow for guided execution."
  log "</user-prompt-submit-hook>"
  log ""
  exit 0
fi

# code-review / review this - run review without commit
if echo "$PROMPT_LOWER" | grep -qE '^(code-review|review\s+this)$'; then
  log ""
  log "<user-prompt-submit-hook>"
  log "REVIEW WORKFLOW TRIGGERED. Execute @review-workflow agent."
  log ""
  log "Steps:"
  log "  1. @code-simplifier → simplify code"
  log "  2. @code-review → code quality review"
  log "  3. @security-scanner → security scan"
  log "  4. Build verification (do NOT commit)"
  log ""
  log "Or invoke: @review-workflow for guided execution."
  log "</user-prompt-submit-hook>"
  log ""
  exit 0
fi

# ============================================================================
# 2. Detect step completion signals
# ============================================================================

if echo "$PROMPT_LOWER" | grep -qE '(planner\s+done|planning\s+done)'; then
  complete_step "planner"
  log ""
  log "[Workflow] Planner step completed!"
  log ""
  exit 0
fi

if echo "$PROMPT_LOWER" | grep -qE '(build\s+pass|build\s+success|build\s+done)'; then
  complete_step "buildPassed"
  log ""
  log "[Workflow] Build step marked as passed!"
  log ""
  exit 0
fi

# ============================================================================
# 3. Detect requirement keywords (suggest planning)
# ============================================================================

needs_planner=false
for kw in "add feature" "create feature" "implement" "new feature" "refactor" "restructure" "architecture" "design system" "integrate" "migration"; do
  if echo "$PROMPT_LOWER" | grep -qF "$kw"; then
    needs_planner=true
    break
  fi
done

# Chinese keywords
if [[ "$needs_planner" == "false" ]]; then
  for kw in "調整" "希望" "增加" "新增"; do
    if echo "$PROMPT" | grep -qF "$kw"; then
      needs_planner=true
      break
    fi
  done
fi

if [[ "$needs_planner" == "true" ]]; then
  state=$(read_state)
  planner_done=$(echo "$state" | jq -r '.completedSteps.planner')
  if [[ "$planner_done" != "true" ]]; then
    log ""
    log "================================================="
    log " PLANNING RECOMMENDED"
    log "================================================="
    log ""
    log " Detected keywords suggesting a complex task."
    log " Consider using @brainstorming or @planning-reviewer first."
    log ""
    log " Or say 'planner done' to skip this step."
    log ""
    log "================================================="
    log ""
  fi
fi

# ============================================================================
# 4. Detect commit intent
# ============================================================================

has_commit_intent=false
for kw in "commit" "done" "ready" "git commit" "push"; do
  if echo "$PROMPT_LOWER" | grep -qF "$kw"; then
    has_commit_intent=true
    break
  fi
done

if [[ "$has_commit_intent" == "true" ]]; then
  state=$(read_state)
  code_file_exists=false

  while IFS= read -r f; do
    [[ -z "$f" ]] && continue
    if is_code_file "$f"; then
      code_file_exists=true
      break
    fi
  done <<< "$(echo "$state" | jq -r '.modifiedFiles[]' 2>/dev/null)"

  if [[ "$code_file_exists" == "true" ]]; then
    missing_steps=$(get_coding_missing_steps)

    if [[ -n "$missing_steps" ]]; then
      log ""
      log "================================================="
      log " WARNING: WORKFLOW STEPS REQUIRED BEFORE COMMIT"
      log "================================================="
      log ""
      log " Missing steps:"
      while IFS= read -r step; do
        [[ -z "$step" ]] && continue
        case "$step" in
          simplifier) log "   [ ] Code Simplifier (@code-simplifier)" ;;
          codeReviewer) log "   [ ] Code Review (@code-review)" ;;
          securityReviewer) log "   [ ] Security Review (@security-scanner)" ;;
        esac
      done <<< "$missing_steps"
      log ""
      log " Complete these steps, then commit will be allowed."
      log " Commands: 'workflow status' | 'workflow skip <step>'"
      log ""
      log "================================================="
      log ""
    else
      log ""
      log "================================================="
      log " ALL WORKFLOW STEPS COMPLETED!"
      log "================================================="
      log " You may now create a commit."
      log "================================================="
      log ""
    fi
  fi
fi

exit 0
