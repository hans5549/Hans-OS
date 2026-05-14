# Workflow - Codex

這份文件定義 Hans-OS 在 Codex 中的手動工作流程。這些規則靠 Codex 執行與回報，不宣稱 repo 內有自動 hook 會代為阻擋。

## Core Principle

- Codex rules are mandatory manual rules, not automatic hooks.
- Do not add fake workflow state or fake automation claims.
- Verification must be reported from commands that actually ran.

## Change Type Rules

| Change type | Definition | Plan | TDD | Review pipeline | Build / typecheck |
|-------------|------------|------|-----|-----------------|-------------------|
| Doc-only | `.md`, `.txt`, `.rst`, `.yml`, `.yaml` | Skip | Skip | Skip | Skip |
| Code change | `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` | Required | Required | Required when dispatchable | Required |

## Required Flow for Code Changes

1. Confirm the current branch is not `main` / `master`.
2. Define success criteria and an executable plan.
3. Read the must-read files from `AGENTS.md`.
4. RED: write a failing test first, or add a test that reproduces the bug.
5. GREEN: write the smallest implementation that makes the test pass.
6. REFACTOR: simplify naming, duplication, flow, and data shape while tests stay green.
7. Run review pipeline when reviewers can actually be dispatched.
8. Run required build, typecheck, and tests.
9. Stage only explicit file paths before committing.

## Goal-Driven Execution

- A plan must include executable steps and verification method.
- A bug fix must define reproduction before the fix.
- A refactor must state expected unchanged behavior and the verification proving it.
- Every new setting, interface, helper, factory, or abstraction must solve a current real problem.
- Trivial doc or typo changes can use shorter verification, but must not expand scope.

## Manual Rules in Codex

### Branch Protection

- Before code changes, check `git status --short --branch`.
- If on `main` / `master`, create or switch to a `feature/*`, `fix/*`, or `refactor/*` branch before editing code.
- Doc-only Codex setting updates may happen on `main` when no code files are touched.

### Commit Gate

Before `git commit`:

- Confirm review, build, tests, and typecheck requirements are satisfied for code changes.
- Use Conventional Commits with Traditional Chinese description.
- Never run `git add .` or `git add -A`.
- Stage only explicit files.

### Post-Edit Build / Typecheck

- Backend code change: run `dotnet build backend/HansOS.slnx` and relevant tests.
- Frontend code change: run `cd frontend && pnpm check:type`.
- Backend + frontend change: run both.
- New or changed API endpoint: run integration tests that cover it.

## Review Pipeline

`.Codex/agents/*` defines reviewer personas. It does not dispatch itself.

### Plan Review

Use when a code task is non-trivial and reviewer dispatch is available:

- `plan-ceo-reviewer`
- `plan-eng-reviewer`
- `plan-linus-reviewer`

If CEO, engineering, and Linus judgments conflict, present the conflict to the user instead of silently blending the answers.

### Code Review

Use after implementation when reviewer dispatch is available:

- `code-review-specialist`
- `security-vuln-scanner`
- `code-simplifier`, when the diff is more than a trivial change
- `linus-reviewer`

If the current environment cannot dispatch reviewers, state `review pipeline not run`. Reading persona files as a checklist is allowed for self-checking, but it does not count as a completed pipeline.

## Validation Matrix

| Change | Required validation |
|--------|---------------------|
| Backend `.cs` / API behavior | `dotnet build backend/HansOS.slnx` plus relevant tests |
| New / changed API endpoint | Integration tests under `backend/tests/HansOS.Api.IntegrationTests` |
| New public service method | Unit or integration tests |
| Bug fix | Reproduction test first, then targeted test pass |
| Frontend `.vue` / `.ts` / `.tsx` | `cd frontend && pnpm check:type` |
| UI behavior change | Typecheck plus manual/browser verification when feasible |
| Deployment / workflow code | Relevant workflow or command dry-run where feasible |
| Doc-only | `git diff --check` or targeted diff review is enough |

## Command Reference

Backend:

```bash
dotnet build backend/HansOS.slnx
dotnet test backend/HansOS.slnx
dotnet run --project backend/src/HansOS.Api
```

Frontend:

```bash
cd frontend && pnpm dev:antd
cd frontend && pnpm check:type
cd frontend && pnpm build:antd
```

EF Core:

```bash
dotnet ef migrations add <Name> --project backend/src/HansOS.Api
dotnet ef database update --project backend/src/HansOS.Api
```

## What This File Does Not Claim

- It does not claim Codex automatically blocks branches, commits, or edits.
- It does not claim Codex automatically builds after edits.
- It does not claim Codex automatically verifies reviewer dispatch.
- It does not create fake workflow state files.

These are manual rules that Codex must follow and report honestly.
