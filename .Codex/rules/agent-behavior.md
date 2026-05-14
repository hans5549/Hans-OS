# Agent Behavior Rules - Codex

這份文件定義 Hans-OS 中跨任務都要遵守的 Codex 行為規則。
它補強 `AGENTS.md` 的短入口，不是外部 12-rule 模板的逐字拷貝。

## Scope

Use this for:

- non-trivial code changes
- multi-step documentation or repo-maintenance tasks
- debugging, refactor, review, and migration work
- any task where assumptions, context size, or verification risk could affect outcome

Trivial one-line doc edits can apply the spirit of these rules without producing a long report.

## Deterministic Work Belongs in Code

- Use Codex judgment for classification, drafting, summarization, extraction, design tradeoffs, and ambiguous requirement analysis.
- Do not use model judgment for deterministic routing, retry policy, status-code handling, schema transforms, permission checks, pagination decisions, or API envelope parsing.
- If an existing contract, status code, type, config, or test can answer the question, follow that deterministic source.
- New model calls or prompt-based decisions inside product code require an explicit product reason, tests, and failure behavior.

## Read Before Writing

Before adding or changing code, read:

1. the target file exports / public surface
2. the immediate caller or consumer
3. the related shared utility, API wrapper, DTO, entity, or route contract
4. the relevant existing tests when behavior already has coverage

Do not add a helper, endpoint, composable, store action, or service method before checking whether an equivalent already exists.
If the current structure is unclear, stop and state the uncertainty before editing.

## Conflicts Must Be Surfaced

When existing patterns contradict, do not average them.

Pick the pattern in this order:

1. explicit rule in `AGENTS.md` or `.Codex/*`
2. current API / database / frontend contract
3. more tested implementation
4. newer implementation in the same module
5. narrower local convention when global convention is absent

Explain the chosen pattern and flag the other as cleanup debt.
Do not refactor the rejected pattern unless the task explicitly includes cleanup.

## Checkpoints and Context

- For multi-step tasks, after each meaningful step summarize: done, verified, left.
- Do not continue from a state you cannot describe precisely.
- If context, token pressure, tool failures, or session length threatens continuity, checkpoint first, then continue or ask for a fresh start.
- When resuming after interruption, restate the current state before making more changes.

## Verification Integrity

- Only claim verification that actually ran.
- If tests, builds, typechecks, reviewer dispatch, visual checks, or edge-case checks were skipped, say `not run` or `skipped` and give the reason.
- Do not convert skipped checks into success language.
- A task is not complete if the requested edge case remains unverified and the user was not told.

## Fail Loud

- Surface uncertainty, partial completion, skipped records, skipped files, and skipped tests.
- Empty catches, swallowed migration failures, ignored import rows, or hidden fallback behavior require explicit logging and a visible result path.
- "Completed" is wrong when meaningful work was skipped silently.
- If a fix depends on an assumption, report the assumption next to the result.
