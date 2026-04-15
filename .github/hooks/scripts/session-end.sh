#!/usr/bin/env bash
# ============================================================================
# session-end.sh - sessionEnd Hook: Session Cleanup & Progress Log
# ============================================================================
# Ported from: .claude/hooks/on-stop.mjs
# Cleans up build lock, reminds about pending steps, writes progress log.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Clean up stale build lock ───────────────────────────────────────────────

if [[ -d "$LOCK_DIR" ]]; then
  rmdir "$LOCK_DIR" 2>/dev/null || true
  log "[Cleanup] Removed stale .build-lock"
fi

# ── Workflow reminder ──────────────────────────────────────────────────────

if [[ -f "$STATE_FILE" ]]; then
  state=$(read_state)
  file_count=$(echo "$state" | jq '.modifiedFiles | length')

  if [[ "$file_count" -gt 0 ]]; then
    pending=""
    for entry in "codeReview:Combined Code Review" "linusReview:Linus Review" "buildPassed:Build"; do
      key="${entry%%:*}"
      name="${entry#*:}"
      val=$(echo "$state" | jq -r --arg k "$key" '.completedSteps[$k]')
      [[ "$val" != "true" ]] && pending="$pending $name,"
    done

    if [[ -n "$pending" ]]; then
      log ""
      log "[Session End] Workflow has pending steps:"
      log "  Tracked files: $file_count"
      log "  Pending:${pending%,}"
      log ""
    fi
  fi
fi

# ── Auto-write session summary to progress.md ──────────────────────────────

if [[ -f "$STATE_FILE" ]]; then
  state=$(read_state)

  branch="(unknown)"
  branch=$(git -C "$PROJECT_ROOT" branch --show-current 2>/dev/null) || true

  completed_steps=$(echo "$state" | jq -r '[.completedSteps | to_entries[] | select(.value == true) | .key] | join(", ")')
  modified_files=$(echo "$state" | jq -r '.modifiedFiles | join(", ")')

  timestamp=$(date -u +"%Y-%m-%dT%H:%M")

  {
    echo ""
    echo "## Session: $timestamp"
    echo "- Branch: $branch"
    echo "- Modified: ${modified_files:-none}"
    echo "- Steps: ${completed_steps:-none}"
    echo ""
  } >> "$WORKFLOW_DIR/progress.md"

  log "[Session] Progress auto-written to progress.md"
fi

exit 0
