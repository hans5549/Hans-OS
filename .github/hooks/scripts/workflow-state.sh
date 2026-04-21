#!/usr/bin/env bash
# ============================================================================
# workflow-state.sh - Workflow State Management (Shared Library)
# ============================================================================
# Ported from: .claude/hooks/workflow-state.mjs
# Provides state tracking for modified files and completed workflow steps.
# Usage: source this file from other hook scripts.
# Requires: jq
# ============================================================================

set -euo pipefail

# ── Paths ───────────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
WORKFLOW_DIR="$PROJECT_ROOT/.github/workflow"
STATE_FILE="$WORKFLOW_DIR/state.json"
LOCK_DIR="$WORKFLOW_DIR/.build-lock"

# ── File extension sets ─────────────────────────────────────────────────────

CODE_EXTENSIONS="cs razor json css js html csproj xml jsx ts tsx vue mts"
DOC_EXTENSIONS="md txt rst yml yaml"

# ── Logging ─────────────────────────────────────────────────────────────────

log() {
  echo "$1" >&2
}

# ── Default state ───────────────────────────────────────────────────────────

default_state() {
  cat <<'EOF'
{
  "modifiedFiles": [],
  "completedSteps": {
    "planner": false,
    "ceoReview": false,
    "engReview": false,
    "planLinusReview": false,
    "codeReview": false,
    "linusReview": false,
    "buildPassed": false
  },
  "lineChangeSinceReview": 0,
  "buildRetryCount": 0,
  "lastModified": "",
  "currentPlanFile": ""
}
EOF
}

# ── Read / Write state ─────────────────────────────────────────────────────

ensure_workflow_dir() {
  mkdir -p "$WORKFLOW_DIR"
}

read_state() {
  ensure_workflow_dir
  if [[ ! -f "$STATE_FILE" ]]; then
    default_state
    return
  fi
  local content
  content=$(cat "$STATE_FILE" 2>/dev/null) || { default_state; return; }
  # Validate JSON
  if ! echo "$content" | jq empty 2>/dev/null; then
    log "[Warning] Invalid state.json, using default"
    default_state
    return
  fi
  # Merge with defaults to ensure all fields exist
  local defaults
  defaults=$(default_state)
  echo "$content" | jq --argjson defaults "$defaults" '
    $defaults * . |
    .modifiedFiles //= [] |
    .completedSteps //= $defaults.completedSteps |
    .completedSteps = ($defaults.completedSteps * .completedSteps) |
    .lineChangeSinceReview //= 0 |
    .buildRetryCount //= 0
  '
}

write_state() {
  local state="$1"
  ensure_workflow_dir
  local now
  now=$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")
  echo "$state" | jq --arg ts "$now" '.lastModified = $ts' > "$STATE_FILE"
}

# ── File type checks ───────────────────────────────────────────────────────

get_extension() {
  local file="$1"
  local ext="${file##*.}"
  echo "${ext,,}"  # lowercase
}

is_code_file() {
  local ext
  ext=$(get_extension "$1")
  for e in $CODE_EXTENSIONS; do
    [[ "$ext" == "$e" ]] && return 0
  done
  return 1
}

is_doc_file() {
  local ext
  ext=$(get_extension "$1")
  for e in $DOC_EXTENSIONS; do
    [[ "$ext" == "$e" ]] && return 0
  done
  return 1
}

# ── File tracking ──────────────────────────────────────────────────────────

add_modified_file() {
  local file_path="$1"
  local line_count="${2:-0}"
  local state
  state=$(read_state)

  # Normalize path
  local normalized
  normalized=$(echo "$file_path" | sed 's|\\|/|g')

  # Exclude workflow infrastructure files
  if echo "$normalized" | grep -q '\.github/'; then
    echo "$state"
    return
  fi

  # Add file if not already tracked
  local already
  already=$(echo "$state" | jq --arg f "$normalized" '.modifiedFiles | index($f)')
  if [[ "$already" == "null" ]]; then
    state=$(echo "$state" | jq --arg f "$normalized" '.modifiedFiles += [$f]')
  fi

  # Handle review step resets for code files
  if is_code_file "$file_path"; then
    state=$(echo "$state" | jq --argjson lc "$line_count" '.lineChangeSinceReview += $lc')

    local review_done
    review_done=$(echo "$state" | jq -r '.completedSteps.codeReview')
    local cum_lines
    cum_lines=$(echo "$state" | jq -r '.lineChangeSinceReview')

    if [[ "$review_done" == "true" ]]; then
      # Code review done → small changes (< 10 lines) warn but preserve
      if [[ "$cum_lines" -ge 10 ]]; then
        state=$(echo "$state" | jq '
          .completedSteps.codeReview = false |
          .completedSteps.linusReview = false |
          .completedSteps.buildPassed = false |
          .lineChangeSinceReview = 0
        ')
      fi
    else
      # Code review not done → reset all coding steps
      state=$(echo "$state" | jq '
        .completedSteps.codeReview = false |
        .completedSteps.linusReview = false |
        .completedSteps.buildPassed = false
      ')
    fi
  fi

  write_state "$state"
  echo "$state"
}

# ── Step management ────────────────────────────────────────────────────────

complete_step() {
  local step_name="$1"
  local state
  state=$(read_state)
  state=$(echo "$state" | jq --arg s "$step_name" '.completedSteps[$s] = true')
  # Reset cumulative line counter when review completes
  if [[ "$step_name" == "codeReview" ]]; then
    state=$(echo "$state" | jq '.lineChangeSinceReview = 0')
  fi
  write_state "$state"
}

reset_step() {
  local step_name="$1"
  local state
  state=$(read_state)
  state=$(echo "$state" | jq --arg s "$step_name" '.completedSteps[$s] = false')
  write_state "$state"
}

reset_workflow_state() {
  local state
  state=$(default_state)
  write_state "$state"
}

# ── Query helpers ──────────────────────────────────────────────────────────

get_coding_missing_steps() {
  local state
  state=$(read_state)
  echo "$state" | jq -r '
    [
      if .completedSteps.codeReview != true then "codeReview" else empty end,
      if .completedSteps.linusReview != true then "linusReview" else empty end
    ] | .[]
  '
}

has_code_files() {
  local state
  state=$(read_state)
  local files
  files=$(echo "$state" | jq -r '.modifiedFiles[]' 2>/dev/null)
  while IFS= read -r f; do
    [[ -z "$f" ]] && continue
    if is_code_file "$f"; then
      return 0
    fi
  done <<< "$files"
  return 1
}

# ── Display ────────────────────────────────────────────────────────────────

show_workflow_status() {
  local state
  state=$(read_state)

  log ""
  log "====== Workflow Status ======"
  log ""

  # Modified files
  log "Modified Files:"
  local file_count
  file_count=$(echo "$state" | jq '.modifiedFiles | length')
  if [[ "$file_count" -eq 0 ]]; then
    log "  (none)"
  else
    echo "$state" | jq -r '.modifiedFiles[]' | while IFS= read -r file; do
      if is_code_file "$file"; then
        log "  [CODE] $file"
      else
        log "  [DOC] $file"
      fi
    done
  fi
  log ""

  # Step status
  log "Completed Steps:"
  log "  -- Planning Phase --"
  local steps=("planner:Planner (optional)" "ceoReview:CEO Review" "engReview:Eng Review" "planLinusReview:Plan Linus Review")
  for entry in "${steps[@]}"; do
    local key="${entry%%:*}"
    local name="${entry#*:}"
    local done_val
    done_val=$(echo "$state" | jq -r --arg k "$key" '.completedSteps[$k]')
    if [[ "$done_val" == "true" ]]; then
      log "  [x] $name"
    else
      log "  [ ] $name"
    fi
  done

  log "  -- Coding Phase --"
  local code_steps=("codeReview:Combined Code Review" "linusReview:Linus Review" "buildPassed:Build Passed")
  for entry in "${code_steps[@]}"; do
    local key="${entry%%:*}"
    local name="${entry#*:}"
    local done_val
    done_val=$(echo "$state" | jq -r --arg k "$key" '.completedSteps[$k]')
    if [[ "$done_val" == "true" ]]; then
      log "  [x] $name"
    else
      log "  [ ] $name"
    fi
  done

  local plan_file
  plan_file=$(echo "$state" | jq -r '.currentPlanFile // ""')
  if [[ -n "$plan_file" ]]; then
    log ""
    log "  Active Plan: $plan_file"
  fi

  log ""
  log "================================"
  log ""
}
