# Workflow — Claude to Codex Mapping

This file translates the actual automation behavior of `Hans-OS/.claude/hooks/*`, `.claude/commands/*`, and `.claude/workflow/*` into a manual workflow that `Codex` can follow.

## Core Principle

- `Claude Code`: has hooks, workflow state, and agent verification, so it can enforce gates automatically.
- `Codex`: has no fully equivalent repo-local hook enforcement.
- Therefore, on the `Codex` side, these rules are **mandatory manual rules**, not automatic blockers.

## Change Type Rules

| Change Type | Definition | Plan Mode | TDD | Review Pipeline | Build / Typecheck |
|-------------|------------|:---------:|:---:|:---------------:|:-----------------:|
| Doc-only | `.md`, `.txt`, `.rst`, `.yml`, `.yaml` | Skip | Skip | Skip | Skip |
| Code change | `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` | Required | Required | Required | Required |

## Required Flow for Code Changes

1. **Confirm you are not editing code directly on `main` / `master`.**
2. **Enter plan mode.**
3. **Select the current task.**
4. **RED**: write a failing test first, or add a test that reproduces the bug.
5. **GREEN**: write the smallest implementation that makes the test pass.
6. **REFACTOR**: improve naming, remove duplication, and reduce complexity.
7. **Review pipeline**
8. **Build / Typecheck / Tests**
9. **Stage only explicit file paths before committing.**

## Goal-Driven Execution

- A plan must translate the requirement into "executable steps + verification method"; high-level descriptions alone are not enough.
- A bug fix must define the reproduction method before the minimal fix.
- A refactor must explicitly state the expected unchanged behavior and list the verification that proves it.
- Trivial doc / typo / obvious one-liner changes may use shorter explanations and narrower verification, but must not expand scope; if it is a code change, it still follows the Change Type Rules above.
- Every new concept, setting, interface, helper, or factory must answer: what real problem does it solve now? Can it be avoided?

## Manual Rules in Codex

### Branch Protection

Claude source:

- `.claude/hooks/pre-edit-check.mjs`

What Claude does:

- Automatically blocks code edits on `main` / `master`.
- Protects `.claude/hooks/*`, `.claude/workflow/state.json`, and `.claude/settings.local.json`.

Codex equivalent rule:

- Before code changes, manually confirm the current branch is not `main` / `master`.
- Do not modify Claude workflow files unless the requirement itself is to maintain Claude automation.

### Commit Gate

Claude source:

- `.claude/hooks/pre-bash-check.mjs`

What Claude does:

- Before `git commit`, checks whether review steps are complete.
- Checks whether backend build / frontend typecheck has run.
- Blocks `git add .` / `git add -A`.

Codex equivalent rule:

- Before committing, manually verify review, build, tests, and typecheck are complete.
- Only run `git add <specific-files>`.

### Post-Edit Build

Claude source:

- `.claude/hooks/post-edit-build.mjs`

What Claude does:

- After `.cs` / `.razor` changes, automatically runs `dotnet build backend/HansOS.slnx --no-restore -v q`.
- After `.vue` / `.ts` / `.tsx` changes, automatically runs `cd frontend && pnpm check:type`.

Codex equivalent rule:

- After backend code changes, manually run the backend build.
- After frontend code changes, manually run `pnpm check:type`.
- If both sides changed, run both.

### Agent Verification

Claude source:

- `.claude/hooks/post-agent-verify.mjs`

What Claude does:

- A workflow step only counts as complete if the agent was actually dispatched.

Codex equivalent rule:

- If claiming the review pipeline is complete, actually run the corresponding reviewer / sub-agent. Do not replace it with the main agent's self-narration.
- `.Codex/agents/*` is the source for reviewer personas and prompt contracts.

## Review Pipeline

### Plan Review

Run in parallel:

- `plan-ceo-reviewer`
- `plan-eng-reviewer`
- `plan-linus-reviewer`

If the CEO review conflicts with the Linus review, do not resolve it automatically. Bring the decision to the user.

### Code Review

Run in parallel:

- `code-review-specialist`
- `security-vuln-scanner`

Then run:

- `linus-reviewer`

## Command Mapping

Existing Claude commands:

- `.claude/commands/commit-this.md`
- `.claude/commands/review-workflow.md`
- `.claude/commands/review-vue.md`

Codex equivalent:

- Do not create fake slash commands.
- Use this file and `.Codex/agents/*` as manual operating instructions.
- The main agent should proactively dispatch reviewers when needed and state the current workflow step in the response.

## Build / Test Matrix

| Change | Required Validation |
|--------|---------------------|
| Backend `.cs` / API behavior | `dotnet build backend/HansOS.slnx` + relevant tests |
| Frontend `.vue` / `.ts` / `.tsx` | `cd frontend && pnpm check:type` |
| New / changed API endpoint | Integration tests |
| New public service method | Unit or Integration tests |
| Bug fix | Reproduction test first |

## Work Artifacts

- `Codex` must not mirror machine state files such as `.claude/workflow/state.json`, `progress.md`, or `findings.md`.
- If work traces are needed, use regular documents or response summaries. Do not create a fake state machine.

## What This File Does Not Claim

- It does not claim that `Codex` automatically blocks branches, commits, or edits.
- It does not claim that `Codex` automatically builds after edits.
- It does not claim that `Codex` automatically verifies whether reviewers actually ran.

These are **mandatory manual rules**, not tool-level enforcement.
