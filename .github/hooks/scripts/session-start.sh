#!/usr/bin/env bash
# ============================================================================
# session-start.sh - sessionStart Hook: Welcome Banner + Workflow Status
# ============================================================================
# Ported from: .claude/hooks/session-start.mjs
# Resets planning phase steps each session. Shows welcome banner.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Clean up stale build lock ───────────────────────────────────────────────

if [[ -d "$LOCK_DIR" ]]; then
  rmdir "$LOCK_DIR" 2>/dev/null && log "[Cleanup] Removed stale .build-lock from previous session" || true
fi

# ── Reset planning phase steps for new session ──────────────────────────────

if [[ -f "$STATE_FILE" ]]; then
  state=$(read_state)

  state=$(echo "$state" | jq '
    .completedSteps.ceoReview = false |
    .completedSteps.engReview = false |
    .completedSteps.planLinusReview = false |
    .currentPlanFile = ""
  ')

  # Reset coding steps if previous session completed (no modified files)
  local_mod_count=$(echo "$state" | jq '.modifiedFiles | length')
  if [[ "$local_mod_count" -eq 0 ]]; then
    state=$(echo "$state" | jq '
      .completedSteps.codeReview = false |
      .completedSteps.linusReview = false |
      .completedSteps.buildPassed = false |
      .lineChangeSinceReview = 0 |
      .buildRetryCount = 0
    ')
  fi

  write_state "$state"
fi

# ── Welcome banner ──────────────────────────────────────────────────────────

log ""
log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log " WORKFLOW AUTOMATION ACTIVE"
log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log ""
log " Coding: Combined Review → Linus → Build → Commit"
log " Planning: CEO Review → Eng Review → Linus Review"
log " Commands: workflow status | workflow reset | commit this"

# ── Branch detection ─────────────────────────────────────────────────────────

current_branch=$(git branch --show-current 2>/dev/null || echo "unknown")

if [[ "$current_branch" == "main" || "$current_branch" == "master" ]]; then
  log " ⚠️  你在 main 分支上。程式變更請先建立 feature branch："
  log "    git checkout -b feature/<name>"
else
  log " 📌 Branch: $current_branch"
fi

log ""

# ── Auto-create progress.md ─────────────────────────────────────────────────

if [[ ! -f "$WORKFLOW_DIR/progress.md" ]]; then
  cat > "$WORKFLOW_DIR/progress.md" << 'PROGRESS'
# Progress Log

> Cross-session progress log. Auto-updated by hooks.

PROGRESS
  log " [Session] Created .github/workflow/progress.md"
fi

# ── Session recovery — show last session summary ────────────────────────────

if [[ -f "$WORKFLOW_DIR/progress.md" ]]; then
  last_session=$(grep -n "^## Session:" "$WORKFLOW_DIR/progress.md" | tail -1 | cut -d: -f1)
  if [[ -n "$last_session" ]]; then
    log " [Recovery] Previous session found. Use workflow status for details."
  fi
fi

# ── Show workflow state if exists ───────────────────────────────────────────

if [[ -f "$STATE_FILE" ]]; then
  state=$(read_state)
  file_count=$(echo "$state" | jq '.modifiedFiles | length')

  if [[ "$file_count" -gt 0 ]]; then
    log " [!] Previous workflow in progress:"
    log "     Tracked files: $file_count"

    done_steps=""
    pending_steps=""
    for entry in "codeReview:Combined Code Review" "linusReview:Linus Review" "buildPassed:Build"; do
      key="${entry%%:*}"
      name="${entry#*:}"
      val=$(echo "$state" | jq -r --arg k "$key" '.completedSteps[$k]')
      if [[ "$val" == "true" ]]; then
        done_steps="$done_steps $name,"
      else
        pending_steps="$pending_steps $name,"
      fi
    done

    [[ -n "$done_steps" ]] && log "     Done:${done_steps%,}"
    [[ -n "$pending_steps" ]] && log "     Pending:${pending_steps%,}"
  fi
fi

log ""
log "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
log ""

exit 0
