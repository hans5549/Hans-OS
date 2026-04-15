#!/usr/bin/env bash
# ============================================================================
# pre-bash-check.sh - preToolUse Hook: Bash Command Guard & Commit Gate
# ============================================================================
# Ported from: .claude/hooks/pre-bash-check.mjs
# Protected files, git add . blocking, commit gating, Conventional Commits.
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/workflow-state.sh"

# ── Read stdin ──────────────────────────────────────────────────────────────

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName // .tool_name // ""' 2>/dev/null) || exit 0

# Only check bash/execute tools
case "$TOOL_NAME" in
  bash|execute|Bash|shell) ;;
  *) exit 0 ;;
esac

COMMAND=$(echo "$INPUT" | jq -r '.toolInput.command // .tool_input.command // ""' 2>/dev/null) || exit 0
[[ -z "$COMMAND" ]] && exit 0

# ============================================================================
# 0. Protected file modification via Bash
# ============================================================================

if echo "$COMMAND" | grep -qE '\.github/hooks/|\.github/workflow/state\.json'; then
  # Allow read-only commands
  if ! echo "$COMMAND" | grep -qE '^(cat|head|tail|less|more|grep|git\s+(diff|log|show))\b'; then
    echo '{"permissionDecision":"deny","permissionDecisionReason":"This command targets a protected workflow file. Hook files and workflow state cannot be modified via Bash."}' 
    exit 0
  fi
fi

# ============================================================================
# 0.5. Block git add . / git add -A
# ============================================================================

if echo "$COMMAND" | grep -qE 'git\s+add\s+(\.|--all|-A)'; then
  echo '{"permissionDecision":"deny","permissionDecisionReason":"git add . / -A is forbidden. Stage specific files only: git add <file1> <file2>"}' 
  exit 0
fi

# ============================================================================
# 0.6. Git merge --no-ff reminder
# ============================================================================

if echo "$COMMAND" | grep -qE 'git\s+merge\b' && ! echo "$COMMAND" | grep -qF -- '--no-ff'; then
  log "[Git] Reminder: Always use --no-ff to preserve branch history."
fi

# ============================================================================
# 1. Check if git commit command
# ============================================================================

if ! echo "$COMMAND" | grep -qE 'git\s+commit'; then
  exit 0
fi

# ============================================================================
# 1.5. Conventional Commits format validation
# ============================================================================

MSG=$(echo "$COMMAND" | sed -n 's/.*-m\s*"\([^"]*\)".*/\1/p')
if [[ -z "$MSG" ]]; then
  MSG=$(echo "$COMMAND" | sed -n "s/.*-m\s*'\([^']*\)'.*/\1/p")
fi

if [[ -n "$MSG" ]]; then
  if ! echo "$MSG" | grep -qE '^(feat|fix|refactor|docs|perf|build|chore|ci|style|test)(\(.+\))?: .+'; then
    log ""
    log "[Git] WARNING: Commit message does not follow Conventional Commits format."
    log "[Git] Expected: <type>(scope): description"
    log "[Git] Types: feat, fix, refactor, docs, perf, build, chore, ci, style, test"
    log ""
  fi
fi

# ============================================================================
# 2. Check for tracked code files
# ============================================================================

state=$(read_state)
code_files_exist=false

while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  if is_code_file "$f"; then
    code_files_exist=true
    break
  fi
done <<< "$(echo "$state" | jq -r '.modifiedFiles[]' 2>/dev/null)"

# If no tracked code files, check git staged files
if [[ "$code_files_exist" == "false" ]]; then
  staged=$(git -C "$PROJECT_ROOT" diff --cached --name-only 2>/dev/null) || staged=""
  if [[ -n "$staged" ]]; then
    while IFS= read -r f; do
      [[ -z "$f" ]] && continue
      [[ "$f" == .github/* ]] && continue
      if is_code_file "$f"; then
        code_files_exist=true
        break
      fi
    done <<< "$staged"
  fi
fi

# Pure doc modification, skip workflow checks
if [[ "$code_files_exist" == "false" ]]; then
  log "[Workflow] No code files detected - skipping workflow checks"
  exit 0
fi

# ============================================================================
# 3. Commit gating — all code changes require full pipeline
# ============================================================================

missing_steps=$(get_coding_missing_steps)

if [[ -n "$missing_steps" ]]; then
  step_list=""
  while IFS= read -r step; do
    [[ -z "$step" ]] && continue
    case "$step" in
      codeReview) step_list="$step_list\n  [ ] Combined Code Review (@code-simplifier + @code-review + @security-scanner)" ;;
      linusReview) step_list="$step_list\n  [ ] Linus Review (@linus-reviewer)" ;;
    esac
  done <<< "$missing_steps"

  echo "{\"permissionDecision\":\"deny\",\"permissionDecisionReason\":\"COMMIT BLOCKED - Workflow steps incomplete!\\nMissing steps:$step_list\\n\\nComplete these steps or use 'workflow skip <step>' to bypass.\"}" 
  exit 0
fi

# ============================================================================
# 4. All steps complete, run final build verification
# ============================================================================

log ""
log "[Workflow] All steps completed! Running final build verification..."

# Backend build
SOLUTION_PATH="backend/HansOS.slnx"
log "[Build] Building backend: $SOLUTION_PATH"

if ! dotnet build "$PROJECT_ROOT/$SOLUTION_PATH" --no-restore -v q 2>&1; then
  reset_step "buildPassed"
  echo '{"permissionDecision":"deny","permissionDecisionReason":"FINAL BUILD FAILED (backend) - Cannot commit! Fix build errors and try again."}' 
  exit 0
fi

log "[Build] Backend build passed"

# Frontend type check (if frontend files were modified)
has_frontend=false
while IFS= read -r f; do
  [[ -z "$f" ]] && continue
  ext="${f##*.}"
  case "$ext" in
    vue|ts|tsx|mts) has_frontend=true; break ;;
  esac
done <<< "$(echo "$state" | jq -r '.modifiedFiles[]' 2>/dev/null)"

if [[ "$has_frontend" == "true" ]]; then
  log "[Build] Checking frontend types..."
  if ! (cd "$PROJECT_ROOT/frontend" && pnpm check:type 2>&1); then
    reset_step "buildPassed"
    echo '{"permissionDecision":"deny","permissionDecisionReason":"FINAL BUILD FAILED (frontend) - Cannot commit! Fix type errors and try again."}' 
    exit 0
  fi
  log "[Build] Frontend type check passed"
fi

# ============================================================================
# 5. Build success — allow commit, reset state
# ============================================================================

log ""
log "[Workflow] Final build passed!"
log "[Workflow] All workflow steps completed - commit allowed"

reset_workflow_state

exit 0
