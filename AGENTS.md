# AGENTS.md — Hans-OS

This file is the primary entry point for `Codex` in the `Hans-OS` repo.
`Claude` still uses the existing `CLAUDE.md` and `.claude/*`; `Codex` should prioritize this file and `.Codex/*`.

## Quick Facts

| Item | Value |
|------|-------|
| **Stack** | .NET 9.0, Vue 3, PostgreSQL, Ant Design Vue, Tailwind CSS |
| **Solution** | `backend/HansOS.slnx` |
| **Backend Build** | `dotnet build backend/HansOS.slnx` |
| **Backend Run** | `dotnet run --project backend/src/HansOS.Api` |
| **Backend Test** | `dotnet test backend/HansOS.slnx` |
| **Frontend Dev** | `cd frontend && pnpm dev:antd` |
| **Frontend Type Check** | `cd frontend && pnpm check:type` |
| **Codex Settings Index** | `.Codex/README.md` |

## Communication

- **Always respond in Traditional Chinese (zh-TW).**
- Keep the tone direct, pragmatic, and executable.
- When uncertain, explicitly state assumptions or gaps. Do not guess.

## AI Coding Guardrails

- When requirements, data flow, or existing contracts are unclear, state assumptions or ask first. Do not fill gaps by guessing.
- Prefer the smallest viable change. Do not add unrequested features, abstractions, settings, or speculative flexibility.
- Only change the scope required by the task. Do not opportunistically refactor, reformat, or edit adjacent unrelated code.
- Every non-trivial code change must define success criteria first, then verify with the corresponding tests, build, or typecheck.

## Codex Must-Read Mapping

Before making changes, read the relevant files based on task type:

| Task Keywords | MUST Read First |
|---------------|-----------------|
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.Codex/ARCHITECTURE.md` |
| EF Core, Migration, DbContext, Entity | `.Codex/rules/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.Codex/rules/review-vue.md` |
| Review, Refactor, Quality | `.Codex/LINUS_MODE.md` |
| Workflow, Plan, Commit, Review Pipeline | `.Codex/rules/workflow.md` |
| Communication, Response Style | `.Codex/rules/communication-style.md` |
| Codex Setting, Agent, Rule, Skill | `.Codex/README.md` |

## Workflow

### Binary Rules

- **Doc-only changes** (`.md`, `.txt`, `.rst`, `.yml`, `.yaml`) may skip TDD, build, and the review pipeline.
- **Code changes** (`.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`) must follow the full workflow.

### Required for Code Changes

1. Enter **plan mode** first and break the requirement into executable steps.
2. Work on a **feature / fix / refactor branch**. Do not modify code directly on `main` / `master`.
3. Follow **RED -> GREEN -> REFACTOR** in order.
4. After completion, run the relevant verification and review pipeline.

### Branch Rules

- Branch naming: `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`
- Although `Codex` may create `codex/*` branches, this repo's project convention takes priority. **Prefer the branch names above.**

### Git Rules

- **Do not use `git add .` or `git add -A`.**
- Stage only explicit file paths.
- Commit messages must use Conventional Commits, with the content written in Traditional Chinese.

## TDD and Tests

**Every new API endpoint and public method MUST have corresponding tests.**

| Change Type | Test Requirement |
|-------------|-----------------|
| New Controller endpoint | Must have corresponding Integration Test |
| New Service public method | Must have Unit Test or Integration Test |
| Modified endpoint behavior | Must update or add corresponding tests |
| Bug fix | Must have a test that reproduces the bug (red-green) |

### Minimum Coverage Per Endpoint

1. Happy path
2. `401 Unauthorized`
3. `400 Bad Request` (if applicable)
4. Business logic edge cases

## Verification Commands

### Backend

```bash
dotnet build backend/HansOS.slnx
dotnet test backend/HansOS.slnx
```

### Frontend

```bash
cd frontend && pnpm check:type
```

- `.cs` / backend behavior changes: run at least the backend build and relevant tests.
- `.vue` / `.ts` / `.tsx` changes: run at least `pnpm check:type`.
- If both frontend and backend are affected, verify both sides.

## Review Pipeline

After code changes are complete, manually follow this review order:

1. Plan Review: run `plan-ceo-reviewer`, `plan-eng-reviewer`, and `plan-linus-reviewer` in parallel.
2. Code Review: run `code-review-specialist` and `security-vuln-scanner` in parallel.
3. Linus Review: run `linus-reviewer`.
4. Build / Typecheck / Tests

Reviewer personas and output contracts are in `.Codex/agents/*`.

## Codex vs Claude

- `Claude` branch protection, commit gate, post-edit build, and agent verification are enforced automatically by `.claude/hooks/*`.
- `Codex` **does not provide equivalent hook enforcement**. Manually follow `.Codex/rules/workflow.md`.
- `CLAUDE.md` remains for `Claude Code`. `Codex` should not modify `.claude/hooks/*`, `.claude/workflow/state.json`, or other Claude automation files unless the user explicitly asks to maintain the Claude workflow itself.
