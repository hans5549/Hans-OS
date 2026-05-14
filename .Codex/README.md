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
| `.Codex/rules/workflow.md` | Manual Codex workflow rules and validation matrix |
| `.Codex/rules/agent-behavior.md` | Cross-task behavior rules: deterministic work, checkpoints, conflicts, verification integrity |
| `.Codex/rules/communication-style.md` | Traditional Chinese response style and work posture |
| `.Codex/rules/csharp-coding-style.md` | Backend C# style, architecture, async, logging, validation rules |
| `.Codex/rules/code-first-ef.md` | EF Core code-first and migration rules |
| `.Codex/rules/project-fit-review-checklist.md` | Hans-OS architecture, auth, EF, menu, Vue, and spec-fit review checklist |
| `.Codex/rules/testing.md` | xUnit, integration test, naming, and coverage rules |
| `.Codex/rules/review-vue.md` | Vue 3 / TypeScript / Ant Design Vue review checklist |
| `.Codex/rules/ui-style-guide.md` | Vben / Ant Design / Tailwind / design token rules |
| `.Codex/agents/*.md` | Plan, code, security, cleanup, and Linus reviewer personas |
| `.Codex/skills/ui-ux-pro-max/*` | Project UI/UX skill and searchable design data |

There is no standalone repo-local `.codex/` or `codex.toml` project settings file. Project-level Codex behavior is defined by `AGENTS.md` and `.Codex/*`.

## Rule Layering

- Keep `AGENTS.md` as the stable short entry point.
- Keep reusable task behavior in `.Codex/rules/agent-behavior.md`.
- Keep workflow gates and validation commands in `.Codex/rules/workflow.md`.
- Keep backend, frontend, EF, testing, UI, and review rules in their scoped rule files.
- Keep repeatable specialist workflows under `.Codex/skills/*` only when they need scripts, data, or reusable assets.
- Do not copy full external templates into the entry point. Adapt only the rules that prevent observed Hans-OS failure modes.

## Source Boundaries

- `Codex`: `AGENTS.md` and `.Codex/*`
- `GitHub Actions`: `.github/workflows/*`

There is no repo-local second AI settings tree. Do not add one unless the user explicitly asks to reintroduce it.
Do not add `CLAUDE.md`, `.claude/*`, `.github/copilot-instructions.md`, or another tool-specific bridge unless the user explicitly asks for multi-tool instruction sharing.

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
3. Read `.Codex/rules/agent-behavior.md` for non-trivial or multi-step work.
4. Read the must-read files listed in `AGENTS.md` for the task type.
5. For code changes, read `.Codex/rules/workflow.md` before editing and define success criteria.
6. For doc-only changes, TDD, build, typecheck, and review pipeline may be skipped, but verify the diff only changes expected documents.

## Task Routing

| Task type | Read first |
|-----------|------------|
| Codex settings cleanup, rule restructuring, agent/skill changes | `.Codex/README.md`, `.Codex/rules/workflow.md`, `.Codex/rules/agent-behavior.md`, `.Codex/rules/communication-style.md` |
| Backend API / service / repository / EF / migration | `.Codex/ARCHITECTURE.md`, `.Codex/rules/csharp-coding-style.md`, `.Codex/rules/code-first-ef.md`, `.Codex/rules/testing.md` |
| Frontend Vue / Pinia / TypeScript / Ant Design Vue | `.Codex/ARCHITECTURE.md`, `.Codex/rules/review-vue.md`, `.Codex/rules/ui-style-guide.md` |
| Code change / multi-step implementation | `.Codex/rules/workflow.md`, `.Codex/rules/agent-behavior.md`, relevant task-specific rule files |
| Refactor / quality / review / guardrails | `.Codex/LINUS_MODE.md`, `.Codex/rules/workflow.md`, `.Codex/rules/agent-behavior.md`, `.Codex/rules/project-fit-review-checklist.md`, relevant `.Codex/agents/*.md` |
| UI/UX design work | `.Codex/skills/ui-ux-pro-max/SKILL.md`, `.Codex/rules/ui-style-guide.md`, `.Codex/rules/review-vue.md` |
| Deployment / CI / Azure | `.Codex/ARCHITECTURE.md`, `docs/deployment.md`, `.github/workflows/*` |

## Workflow Rules

- Doc-only changes: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` may skip TDD, build, typecheck, and review pipeline.
- Code changes: `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` require the full workflow.
- Code changes must not be made directly on `main` / `master`.
- Bug fixes require a reproducing test first.
- New API endpoints and new public service methods require corresponding tests.
- Non-trivial code changes need success criteria and a verification loop.
- Multi-step work needs checkpoints for done, verified, and left.
- Skipped verification, reviewer dispatch, or edge-case checks must be reported explicitly.
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

Do not describe unimplemented automation as available. If a review step cannot actually be dispatched in the current Codex environment, report that honestly.

## Maintenance Checklist

When adjusting Codex settings:

- Keep `AGENTS.md` as a short entry point; put detail under `.Codex/*`.
- When adding a must-read file, update both `AGENTS.md` and this README.
- When changing cross-task behavior, update `.Codex/rules/agent-behavior.md` and only summarize the highest-priority effects in `AGENTS.md`.
- When changing build / test / typecheck commands, update `AGENTS.md`, `.Codex/rules/workflow.md`, and any architecture notes.
- When changing backend patterns, update `.Codex/ARCHITECTURE.md`, `.Codex/rules/csharp-coding-style.md`, and `.Codex/rules/code-first-ef.md` if relevant.
- When changing frontend design or UI rules, update `.Codex/rules/review-vue.md` and `.Codex/rules/ui-style-guide.md`.
- When changing the reviewer flow, update `.Codex/rules/workflow.md` and `.Codex/agents/*`.
- After completion, run `git diff -- AGENTS.md .Codex .github` to verify that only expected files changed.
