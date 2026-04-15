#!/usr/bin/env bash
# ============================================================================
# post-tool-track.sh - postToolUse Hook: Modification Tracker + Smart Reset
# ============================================================================
# Tracks file modifications after tool use. Handles Smart Reset logic:
# - < 10 lines cumulative after review → warning only, reviews preserved
# - >= 10 lines → reset codeReview + linusReview + buildPassed
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

# Normalize
FILE_PATH=$(echo "$FILE_PATH" | sed 's|\\|/|g')

# Skip workflow infrastructure files
if echo "$FILE_PATH" | grep -qE '\.github/'; then
  exit 0
fi

# ── Track modification ─────────────────────────────────────────────────────

if is_code_file "$FILE_PATH" || is_doc_file "$FILE_PATH"; then
  # Estimate line count
  LINE_COUNT=0
  NEW_CONTENT=$(echo "$INPUT" | jq -r '.toolInput.new_string // .tool_input.new_string // .toolInput.content // .tool_input.content // ""' 2>/dev/null) || true
  if [[ -n "$NEW_CONTENT" ]]; then
    LINE_COUNT=$(echo "$NEW_CONTENT" | wc -l | tr -d ' ')
  fi

  add_modified_file "$FILE_PATH" "$LINE_COUNT" > /dev/null
fi

exit 0
