#!/usr/bin/env bash
# ============================================================================
# post-edit-build.sh - postToolUse Hook: Auto Build on Code File Edit
# ============================================================================
# Ported from: .claude/hooks/post-edit-build.mjs
# Dual build: Backend (.cs) → dotnet build, Frontend (.vue/.ts) → pnpm check:type
# Atomic directory lock, retry count (max 5), strike tracking.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Read stdin ──────────────────────────────────────────────────────────────

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName // .tool_name // ""' 2>/dev/null) || exit 0

# Only trigger on edit/create tools
case "$TOOL_NAME" in
  edit|create|Edit|MultiEdit|Write) ;;
  *) exit 0 ;;
esac

# ── Extract file path ──────────────────────────────────────────────────────

FILE_PATH=$(echo "$INPUT" | jq -r '.toolInput.path // .tool_input.file_path // .tool_input.path // ""' 2>/dev/null) || exit 0
[[ -z "$FILE_PATH" ]] && exit 0

# ── Determine build type ───────────────────────────────────────────────────

EXT="${FILE_PATH##*.}"
EXT=$(echo "$EXT" | tr '[:upper:]' '[:lower:]')

BUILD_TYPE=""
case "$EXT" in
  cs|razor) BUILD_TYPE="backend" ;;
  vue|ts|tsx) BUILD_TYPE="frontend" ;;
esac

[[ -z "$BUILD_TYPE" ]] && exit 0

# ── Atomic lock ─────────────────────────────────────────────────────────────

if ! mkdir "$LOCK_DIR" 2>/dev/null; then
  log "[Build] Skipping — another build is in progress"
  exit 0
fi

# Ensure lock is released on exit
cleanup_lock() {
  rmdir "$LOCK_DIR" 2>/dev/null || true
}
trap cleanup_lock EXIT

# ── Run build ───────────────────────────────────────────────────────────────

BUILD_OK=false

if [[ "$BUILD_TYPE" == "backend" ]]; then
  SOLUTION_PATH="$PROJECT_ROOT/backend/HansOS.slnx"
  log "[Build] Auto-building backend after .$EXT edit..."

  if dotnet build "$SOLUTION_PATH" --no-restore -v q 2>&1 | tail -5 >&2; then
    log "[Build] Backend build passed"
    BUILD_OK=true
  fi
else
  log "[Build] Auto-checking frontend types after .$EXT edit..."

  if (cd "$PROJECT_ROOT/frontend" && pnpm check:type 2>&1 | tail -5 >&2); then
    log "[Build] Frontend type check passed"
    BUILD_OK=true
  fi
fi

# ── Update state ────────────────────────────────────────────────────────────

state=$(read_state)

if [[ "$BUILD_OK" == "true" ]]; then
  complete_step "buildPassed"

  # Reset retry count on success
  retry_count=$(echo "$state" | jq -r '.buildRetryCount // 0')
  if [[ "$retry_count" -gt 0 ]]; then
    state=$(echo "$state" | jq '.buildRetryCount = 0')
    write_state "$state"
  fi
else
  # Build failed — increment retry count
  state=$(echo "$state" | jq '.buildRetryCount = (.buildRetryCount // 0) + 1')

  # Record in editHistory
  NOW=$(date +%s)000
  state=$(echo "$state" | jq --arg f "$FILE_PATH" --argjson t "$NOW" '
    .editHistory += [{"file": $f, "time": $t, "buildOk": false}] |
    if (.editHistory | length) > 10 then .editHistory = .editHistory[-10:] else . end
  ')

  # Strike system
  retry_count=$(echo "$state" | jq -r '.buildRetryCount // 0')

  if [[ "$retry_count" -eq 3 ]]; then
    log ""
    log "━━━━ Strike 2: Build failed 3 times ━━━━"
    log "Current approach may have a fundamental problem:"
    log "  1. STOP — analyze why the fixes are not working"
    log "  2. Try a completely different approach"
    log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  fi

  if [[ "$retry_count" -ge 5 ]]; then
    log ""
    log "━━━━ Strike 3: Build failed 5+ times ━━━━"
    log "Escalation threshold reached:"
    log "  1. STOP — do not retry the same direction"
    log "  2. Report to user: what was tried + what was learned"
    log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  fi

  write_state "$state"
fi

exit 0
