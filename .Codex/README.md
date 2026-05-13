# Codex Settings - Hans-OS

這個資料夾是 Hans-OS repo 的 Codex 專用設定、規則、審查 persona 與專案索引。
主要入口是 repo root 的 `AGENTS.md`；詳細規則放在 `.Codex/*`。

## Current Layout

| Path | Purpose |
|------|---------|
| `AGENTS.md` | Codex entry point: quick facts, must-read mapping, workflow boundaries |
| `.Codex/README.md` | This file: Codex settings index and maintenance rules |
| `.Codex/ARCHITECTURE.md` | Current backend, frontend, database, auth, deployment, and module map |
| `.Codex/LINUS_MODE.md` | Linus-style judgment and review principles |
| `.Codex/rules/workflow.md` | Manual Codex workflow mapped from Claude hook-based workflows |
| `.Codex/rules/communication-style.md` | Traditional Chinese response style and work posture |
| `.Codex/rules/csharp-coding-style.md` | Backend C# style, architecture, async, logging, validation rules |
| `.Codex/rules/code-first-ef.md` | EF Core code-first and migration rules |
| `.Codex/rules/testing.md` | xUnit, integration test, naming, and coverage rules |
| `.Codex/rules/review-vue.md` | Vue 3 / TypeScript / Ant Design Vue review checklist |
| `.Codex/rules/ui-style-guide.md` | Vben / Ant Design / Tailwind / design token rules |
| `.Codex/agents/*.md` | Plan, code, security, cleanup, and Linus reviewer personas |
| `.Codex/skills/ui-ux-pro-max/*` | Project UI/UX skill and searchable design data |

There is no standalone repo-local `.codex/` or `codex.toml` project settings file. Project-level Codex behavior is defined by `AGENTS.md` and `.Codex/*`.

## Source Boundaries

- `Codex`: `AGENTS.md` and `.Codex/*`
- `Claude Code`: `CLAUDE.md` and `.claude/*`
- `GitHub Actions`: `.github/workflows/*`

Codex may read `.claude/*` as reference material, but should not edit Claude hook state or automation files unless the user explicitly asks for that workflow to be maintained. `.github/*` is limited to GitHub Actions deployment workflows.

Protected unless explicitly requested:

- `.claude/hooks/*`
- `.claude/workflow/state.json`
- `.claude/settings.local.json`

## Current Project Facts

- Backend solution: `backend/HansOS.slnx`
- Backend API project: `backend/src/HansOS.Api`
- Test projects: `backend/tests/HansOS.Api.UnitTests`, `backend/tests/HansOS.Api.IntegrationTests`
- Frontend app: `frontend/apps/web-antd`
- Package manager: `pnpm@10.30.3`
- Backend deploy target: Azure App Service `hans-os-api`
- Frontend deploy target: Azure Static Web Apps
- Health endpoints: `/healthz`, `/readyz`, `/health`

## Read Order

1. Read `AGENTS.md`.
2. Read `.Codex/README.md` when the task touches Codex settings, agents, rules, or skills.
3. Read the must-read files listed in `AGENTS.md` for the task type.
4. For code changes, read `.Codex/rules/workflow.md` before editing and define success criteria.
5. For doc-only changes, TDD, build, typecheck, and review pipeline may be skipped, but verify the diff only changes expected documents.

## Task Routing

| Task type | Read first |
|-----------|------------|
| Codex settings cleanup, rule restructuring, agent/skill changes | `.Codex/README.md`, `.Codex/rules/workflow.md`, `.Codex/rules/communication-style.md` |
| Backend API / service / repository / EF / migration | `.Codex/ARCHITECTURE.md`, `.Codex/rules/csharp-coding-style.md`, `.Codex/rules/code-first-ef.md`, `.Codex/rules/testing.md` |
| Frontend Vue / Pinia / TypeScript / Ant Design Vue | `.Codex/ARCHITECTURE.md`, `.Codex/rules/review-vue.md`, `.Codex/rules/ui-style-guide.md` |
| Refactor / quality / review / guardrails | `.Codex/LINUS_MODE.md`, `.Codex/rules/workflow.md`, relevant `.Codex/agents/*.md` |
| UI/UX design work | `.Codex/skills/ui-ux-pro-max/SKILL.md`, `.Codex/rules/ui-style-guide.md`, `.Codex/rules/review-vue.md` |
| Deployment / CI / Azure | `.Codex/ARCHITECTURE.md`, `docs/deployment.md`, `.github/workflows/*` |

## Workflow Rules

- Doc-only changes: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` may skip TDD, build, typecheck, and review pipeline.
- Code changes: `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` require the full workflow.
- Code changes must not be made directly on `main` / `master`.
- Bug fixes require a reproducing test first.
- New API endpoints and new public service methods require corresponding tests.
- Non-trivial code changes need success criteria and a verification loop.
- Before committing, do not use `git add .` or `git add -A`; stage only explicit file paths.

## Review Pipeline Notes

`.Codex/agents/*` defines reviewer personas and output contracts. These files are not automatic hooks.

If claiming the review pipeline is complete, the corresponding reviewer or equivalent dispatch flow must actually run. If the current tool environment cannot dispatch reviewers, explicitly state `review pipeline not run`; do not replace it with the main agent's self-review.

Recommended order:

1. Plan review: `plan-ceo-reviewer`, `plan-eng-reviewer`, `plan-linus-reviewer`
2. Code review: `code-review-specialist`, `security-vuln-scanner`
3. Cleanup review: `code-simplifier`, when the diff is larger than a trivial change
4. Final taste review: `linus-reviewer`
5. Build / typecheck / tests

Claude-only wrappers such as `plan-codex-adversarial-reviewer`, `gatex-codex-reviewer`, and `stop-review-gate` are not mirrored as Codex agents because they exist to call Codex from Claude.

## Maintenance Checklist

When adjusting Codex settings:

- Keep `AGENTS.md` as a short entry point; put detail under `.Codex/*`.
- When adding a must-read file, update both `AGENTS.md` and this README.
- When changing build / test / typecheck commands, update `AGENTS.md`, `.Codex/rules/workflow.md`, and any architecture notes.
- When changing backend patterns, update `.Codex/ARCHITECTURE.md`, `.Codex/rules/csharp-coding-style.md`, and `.Codex/rules/code-first-ef.md` if relevant.
- When changing frontend design or UI rules, update `.Codex/rules/review-vue.md` and `.Codex/rules/ui-style-guide.md`.
- When changing the reviewer flow, update `.Codex/rules/workflow.md` and `.Codex/agents/*`.
- After completion, run `git diff -- AGENTS.md .Codex .github` to verify that only expected files changed.
