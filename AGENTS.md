# AGENTS.md - Hans-OS

這是 `Codex` 在 `Hans-OS` repo 的主要入口。
`Claude` 仍使用 `CLAUDE.md` 與 `.claude/*`；`.github/*` 只保留 GitHub Actions；`Codex` 優先讀本檔與 `.Codex/*`。

## Quick Facts

| Item | Value |
|------|-------|
| Stack | .NET 9.0, Vue 3, Vben Admin 5.6, PostgreSQL, Ant Design Vue, Tailwind CSS |
| Backend solution | `backend/HansOS.slnx` |
| Backend build | `dotnet build backend/HansOS.slnx` |
| Backend run | `dotnet run --project backend/src/HansOS.Api` |
| Backend test | `dotnet test backend/HansOS.slnx` |
| Frontend dev | `cd frontend && pnpm dev:antd` |
| Frontend typecheck | `cd frontend && pnpm check:type` |
| Frontend build | `cd frontend && pnpm build:antd` |
| Frontend package manager | `pnpm@10.30.3` |
| Health endpoints | `/healthz`, `/readyz`, `/health` |
| Codex settings index | `.Codex/README.md` |

> There is no standalone lint command in the repo-level workflow. Do not invent one.

## Communication

- 一律使用繁體中文（zh-TW）回覆。
- 語氣直接、務實、可執行。
- 不確定需求、資料流或既有契約時，先明確列出假設或提問；不要猜。
- 回覆順序優先：事實、判斷、下一步。

## Project Snapshot

- Backend: ASP.NET Core Web API, EF Core Code-First, PostgreSQL, ASP.NET Identity, JWT access token + HttpOnly refresh token cookie。
- Backend modules: auth/user/menu, TSF settings, bank transactions, annual budget, budget share, activities, pending remittances, finance tasks, personal finance accounts/categories/transactions/stocks, API spec。
- Frontend: `frontend/apps/web-antd`，Vben Admin backend access mode，API base URL 由 `VITE_GLOB_API_URL` 控制。
- Frontend routing: local static demo routes plus backend-driven menu/routes through `getAllMenusApi()` and `generateAccessible()`。
- Tests: xUnit unit tests and integration tests under `backend/tests/*`，integration tests use `WebApplicationFactory<Program>` with EF InMemory override。
- Deployment: backend to Azure App Service, frontend to Azure Static Web Apps, both triggered from `main` by GitHub Actions.

## AI Coding Guardrails

- Prefer the smallest viable change.
- Do not add unrequested features, abstractions, settings, or speculative flexibility.
- Only change the scope required by the task.
- Do not opportunistically refactor, reformat, or edit unrelated code.
- Every non-trivial code change must define success criteria first, then verify with the corresponding tests, build, or typecheck.
- New API endpoints and new public service methods require tests.

## Codex Must-Read Mapping

Before making changes, read the relevant files based on task type:

| Task keywords | MUST read first |
|---------------|-----------------|
| Codex setting, agent, rule, skill | `.Codex/README.md`, `.Codex/rules/workflow.md`, `.Codex/rules/communication-style.md` |
| Entity, Service, Controller, API, DbContext, Migration | `.Codex/ARCHITECTURE.md`, `.Codex/rules/csharp-coding-style.md` |
| EF Core, Migration, DbContext, Entity | `.Codex/rules/code-first-ef.md` |
| C#, backend behavior, dependency injection | `.Codex/rules/csharp-coding-style.md`, `.Codex/ARCHITECTURE.md` |
| Backend tests, integration tests, bug fix | `.Codex/rules/testing.md`, `.Codex/rules/workflow.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.Codex/ARCHITECTURE.md`, `.Codex/rules/review-vue.md` |
| UI, styling, design tokens, Ant Design appearance | `.Codex/rules/ui-style-guide.md`, `.Codex/rules/review-vue.md` |
| Review, refactor, quality, taste | `.Codex/LINUS_MODE.md`, `.Codex/rules/workflow.md`, relevant `.Codex/agents/*.md` |
| Deployment, CI, Azure, GitHub Actions | `.Codex/ARCHITECTURE.md`, `docs/deployment.md`, `.github/workflows/*` |
| Communication, response style | `.Codex/rules/communication-style.md` |

## Workflow

### Binary Rules

- Doc-only changes (`.md`, `.txt`, `.rst`, `.yml`, `.yaml`) may skip TDD, build, typecheck, and the review pipeline.
- Code changes (`.cs`, `.vue`, `.ts`, `.tsx`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`) must follow the full workflow.

### Required for Code Changes

1. Define success criteria and an executable plan first.
2. Work on a feature, fix, or refactor branch. Do not modify code directly on `main` or `master`.
3. Follow RED -> GREEN -> REFACTOR in order.
4. Run the relevant review pipeline when the environment can actually dispatch reviewers.
5. Run the required build, typecheck, and tests.

### Branch Rules

- Branch naming: `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`
- Repo convention takes priority over Codex's default `codex/*` branch prefix.

### Git Rules

- Do not use `git add .` or `git add -A`.
- Stage only explicit file paths.
- Commit messages must use Conventional Commits, with the content written in Traditional Chinese.

## TDD and Tests

Every new API endpoint and public method MUST have corresponding tests.

| Change type | Test requirement |
|-------------|------------------|
| New Controller endpoint | Integration test |
| New Service public method | Unit test or integration test |
| Modified endpoint behavior | Update or add corresponding tests |
| Bug fix | Test that reproduces the bug first |

Minimum endpoint coverage:

1. Happy path
2. `401 Unauthorized`
3. `400 Bad Request`, if applicable
4. Business logic edge cases

## Verification Commands

Backend:

```bash
dotnet build backend/HansOS.slnx
dotnet test backend/HansOS.slnx
```

Frontend:

```bash
cd frontend && pnpm check:type
```

- `.cs` / backend behavior changes: run at least backend build and relevant tests.
- `.vue` / `.ts` / `.tsx` changes: run at least `pnpm check:type`.
- If both frontend and backend are affected, verify both sides.

## Review Pipeline

`.Codex/agents/*` contains reviewer personas and output contracts. It is not an automatic hook system.

When a code change requires review and the current Codex environment can actually dispatch reviewers:

1. Plan review: `plan-ceo-reviewer`, `plan-eng-reviewer`, `plan-linus-reviewer`
2. Code review: `code-review-specialist`, `security-vuln-scanner`
3. Cleanup review when useful: `code-simplifier`
4. Final taste review: `linus-reviewer`
5. Build / typecheck / tests

If reviewers cannot be dispatched, explicitly say the review pipeline was not run. Reading the persona files as checklists is useful, but it does not count as a completed reviewer run.

## Codex vs Claude vs GitHub

- Codex uses `AGENTS.md` and `.Codex/*`.
- Claude uses `CLAUDE.md` and `.claude/*`.
- GitHub Actions uses `.github/workflows/*`.
- Do not modify `.claude/hooks/*`, `.claude/workflow/state.json`, or `.claude/settings.local.json` unless the user explicitly asks to maintain the Claude workflow.
