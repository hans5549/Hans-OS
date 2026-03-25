#!/usr/bin/env bash
# ============================================================================
# pre-edit-check.sh - preToolUse Hook: File Edit Guard
# ============================================================================
# Ported from: .claude/hooks/pre-edit-check.mjs
# Main branch protection, protected files, file tracking.
# Tool names: edit, create (Copilot CLI uses lowercase)
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Read stdin ──────────────────────────────────────────────────────────────

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName // .tool_name // ""' 2>/dev/null) || exit 0

# Only track edit/create tools
case "$TOOL_NAME" in
  edit|create|Edit|MultiEdit|Write) ;;
  *) exit 0 ;;
esac

# ── Extract file path ──────────────────────────────────────────────────────

FILE_PATH=$(echo "$INPUT" | jq -r '.toolInput.path // .tool_input.file_path // .tool_input.path // ""' 2>/dev/null) || exit 0
[[ -z "$FILE_PATH" ]] && exit 0

# Normalize path
FILE_PATH=$(echo "$FILE_PATH" | sed 's|\\|/|g')

# ============================================================================
# 1. Main Branch Protection
# ============================================================================

# Allow workflow file writes on main
IS_WORKFLOW_FILE=false
if echo "$FILE_PATH" | grep -qE '\.(github|claude)/(plans|reviews|test-plans|specs|workflow)/'; then
  IS_WORKFLOW_FILE=true
fi

BRANCH=$(git -C "$PROJECT_ROOT" branch --show-current 2>/dev/null) || BRANCH=""

if [[ ("$BRANCH" == "main" || "$BRANCH" == "master") && "$IS_WORKFLOW_FILE" == "false" ]]; then
  echo '{"permissionDecision":"deny","permissionDecisionReason":"Cannot edit files on main branch. Create a feature branch first: git checkout -b feature/xxx"}' 
  exit 0
fi

# ============================================================================
# 2. Protected Files
# ============================================================================

if echo "$FILE_PATH" | grep -qE '\.github/hooks/|\.github/workflow/state\.json'; then
  echo '{"permissionDecision":"deny","permissionDecisionReason":"Protected workflow file. Hook files and workflow state cannot be modified during a session."}' 
  exit 0
fi

# ============================================================================
# 3. Sensitive File Warning
# ============================================================================

if echo "$FILE_PATH" | grep -qiE 'appsettings.*\.json$'; then
  log "[WARNING] Editing appsettings.json — connection strings, JWT keys should be set via environment variables."
fi

# ============================================================================
# 4. Track File Modification
# ============================================================================

if is_code_file "$FILE_PATH" || is_doc_file "$FILE_PATH"; then
  # Estimate line count from edit content
  LINE_COUNT=0
  NEW_CONTENT=$(echo "$INPUT" | jq -r '.toolInput.new_string // .tool_input.new_string // .toolInput.content // .tool_input.content // ""' 2>/dev/null) || true
  if [[ -n "$NEW_CONTENT" ]]; then
    LINE_COUNT=$(echo "$NEW_CONTENT" | wc -l | tr -d ' ')
  fi

  state=$(add_modified_file "$FILE_PATH" "$LINE_COUNT")

  if is_code_file "$FILE_PATH"; then
    code_count=$(echo "$state" | jq '[.modifiedFiles[] | select(test("\\.(cs|razor|json|css|js|html|csproj|xml|jsx|ts|tsx|vue|mts)$"))] | length')
    cum_lines=$(echo "$state" | jq -r '.lineChangeSinceReview // 0')
    simplifier_done=$(echo "$state" | jq -r '.completedSteps.simplifier')

    if [[ "$simplifier_done" == "true" && "$cum_lines" -gt 0 && "$cum_lines" -lt 10 ]]; then
      log "[Workflow] Tracking code edit: $FILE_PATH (+$LINE_COUNT lines, cumulative $cum_lines/10 — reviews preserved)"
    else
      log "[Workflow] Tracking code file: $FILE_PATH | Total code files: $code_count"
      if [[ "$simplifier_done" != "true" || "$cum_lines" -ge 10 ]]; then
        log "[Workflow] Review steps have been reset"
      fi
    fi
  fi
fi

exit 0
