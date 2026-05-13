# Codex Settings — Hans-OS

This directory contains project-level settings for `Codex` in the Hans-OS repo.
`AGENTS.md` is the primary entry point; `.Codex/*` contains fuller rules, reviewer personas, architecture references, and project skills.

## Current Layout

| Path | Purpose |
|------|---------|
| `AGENTS.md` | Codex entry point: quick facts, must-read mapping, and workflow boundaries |
| `.Codex/README.md` | This file: Codex settings index and maintenance rules |
| `.Codex/ARCHITECTURE.md` | Backend, frontend, database, auth, and deployment architecture summary |
| `.Codex/LINUS_MODE.md` | Linus-style judgment and review principles |
| `.Codex/rules/workflow.md` | Mapping from Claude hooks to the manual Codex workflow |
| `.Codex/rules/communication-style.md` | Traditional Chinese response style, work posture, and foundation/integration explanation rules |
| `.Codex/rules/code-first-ef.md` | EF Core code-first and migration rules |
| `.Codex/rules/review-vue.md` | Vue 3 / TypeScript / Ant Design Vue review checklist |
| `.Codex/agents/*.md` | Plan/code/security/Linus reviewer personas and output contracts |
| `.Codex/skills/ui-ux-pro-max/*` | Project UI/UX skill and searchable design data |

This repo currently has no standalone `.codex/` or `codex.toml` project settings file. Project-level Codex rules are defined by `AGENTS.md` and `.Codex/*`.

## Hans-OS AI Coding Guardrails

The AI Coding Guardrails in `AGENTS.md` are Hans-OS-local rules used to constrain Codex's day-to-day planning, implementation, and review behavior.

- This is not an external plugin import, and it is not a verbatim copy.
- Do not add `.claude-plugin/*`, `.cursor/*`, or modify `.claude/*`.
- Keep the short entry rules in `AGENTS.md`; put detailed checks in `.Codex/rules/workflow.md`, `.Codex/LINUS_MODE.md`, and `.Codex/agents/*`.
- The core standards are: clarify first, keep it simple, make surgical changes, and verify completion.

## Read Order

1. Read `AGENTS.md` first.
2. Read the must-read files from `AGENTS.md` based on task type.
3. If the task involves Codex settings, workflow, reviewers, agents, or skill maintenance, read this file.
4. For code changes, read `.Codex/rules/workflow.md` and follow the TDD / review / validation flow.
5. For doc-only changes, TDD, build, and the review pipeline may be skipped, but still check that the diff only changes documents.

## Task Routing

| Task Type | Read First |
|-----------|------------|
| Codex settings cleanup, rule restructuring, agent/skill changes | `.Codex/README.md`, `.Codex/rules/workflow.md`, `.Codex/rules/communication-style.md` |
| Backend API / service / repository / EF / migration | `.Codex/ARCHITECTURE.md`, `.Codex/rules/code-first-ef.md`, `.Codex/rules/workflow.md` |
| Frontend Vue / Pinia / TypeScript / Ant Design Vue | `.Codex/ARCHITECTURE.md`, `.Codex/rules/review-vue.md`, `.Codex/rules/workflow.md` |
| Refactor / quality / review / guardrails | `.Codex/LINUS_MODE.md`, `.Codex/rules/workflow.md`, relevant `.Codex/agents/*.md` |
| UI/UX design work | `.Codex/skills/ui-ux-pro-max/SKILL.md`, `.Codex/rules/review-vue.md` |

## Workflow Rules

- Doc-only changes: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` may skip TDD, build, and the review pipeline.
- Code changes: `.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml` must follow the full workflow.
- Code changes must not be made directly on `main` / `master`.
- Bug fixes require a reproducing test first; new API endpoints / public methods require corresponding tests.
- Non-trivial code changes need success criteria and a verification loop; trivial changes may use shorter explanations, but must not expand scope.
- Before committing, do not use `git add .` or `git add -A`; stage only explicit file paths.

## Review Pipeline Notes

`.Codex/agents/*` defines reviewer personas and output contracts. It is not an automatic hook.

If claiming the review pipeline is complete, the corresponding reviewer or equivalent review flow must actually be run. If the current tool environment cannot dispatch sub-agents / reviewers, explicitly state "review pipeline not run"; do not replace it with the main agent's self-narration.

Recommended order:

1. Plan Review: `plan-ceo-reviewer`, `plan-eng-reviewer`, `plan-linus-reviewer`
2. Code Review: `code-review-specialist`, `security-vuln-scanner`
3. Linus Review: `linus-reviewer`
4. Build / typecheck / tests

## Claude Boundary

- `Claude` uses `CLAUDE.md` and `.claude/*`.
- `Codex` uses `AGENTS.md` and `.Codex/*`.
- Do not modify `.claude/hooks/*`, `.claude/workflow/state.json`, or `.claude/settings.local.json` unless the user explicitly asks to maintain the Claude workflow.
- Do not create a fake hook state machine inside `.Codex`; Codex-side work traces should be regular documents or response summaries.

## Maintenance Checklist

When adjusting Codex settings, check each item:

- Keep `AGENTS.md` as a short entry point. Put detailed rules under `.Codex/*`.
- When adding a must-read file, update both the `AGENTS.md` mapping and this file.
- When changing AI Coding Guardrails, keep only hard entry rules in `AGENTS.md`; put details in workflow / Linus / reviewer files.
- When changing build / test / typecheck commands, update both `AGENTS.md` and `.Codex/rules/workflow.md`.
- When changing the reviewer flow, update `.Codex/rules/workflow.md` and `.Codex/agents/*`.
- When adding UI/UX skill data or scripts, ensure `.Codex/skills/ui-ux-pro-max/SKILL.md` still describes actual usage.
- After completion, run `git diff -- AGENTS.md .Codex` to verify that only expected files changed.
