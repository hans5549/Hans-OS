#!/usr/bin/env bash
# ============================================================================
# error-handler.sh - errorOccurred Hook: Error Logging
# ============================================================================
# Copilot CLI exclusive (Claude Code has no errorOccurred hook).
# Records errors to workflow state for tracking build failures.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Read stdin ──────────────────────────────────────────────────────────────

INPUT=$(cat)
ERROR_MSG=$(echo "$INPUT" | jq -r '.error // .message // "Unknown error"' 2>/dev/null) || exit 0

# Log to stderr (visible to user)
log "[Error] $ERROR_MSG"

# ── Track build-related errors ──────────────────────────────────────────────

if echo "$ERROR_MSG" | grep -qiE 'build|compile|type.?check|dotnet|pnpm'; then
  state=$(read_state)
  state=$(echo "$state" | jq '.buildRetryCount = (.buildRetryCount // 0) + 1')
  retry_count=$(echo "$state" | jq -r '.buildRetryCount')

  if [[ "$retry_count" -ge 5 ]]; then
    log "[Error] Build has failed $retry_count times. Consider stopping and analyzing the root cause."
  fi

  write_state "$state"
fi

exit 0
