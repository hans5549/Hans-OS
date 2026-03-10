# CLAUDE.md — Hans-OS (CGMSportFinance)

## Quick Facts

| Item | Value |
|------|-------|
| **Stack** | .NET 9.0, Vue 3, PostgreSQL, Ant Design Vue, Tailwind CSS |
| **Solution** | `backend/CGMSportFinance.sln` |
| **Backend Build** | `dotnet build backend/CGMSportFinance.sln` |
| **Backend Run** | `dotnet run --project backend/src/CGMSportFinance.Api` |
| **Backend Test** | `dotnet test backend/CGMSportFinance.sln` |
| **Frontend Dev** | `cd frontend && pnpm dev:antd` |
| **Frontend Type Check** | `cd frontend && pnpm check:type` |

---

## Build / Run / Test

```bash
# Build backend
dotnet build backend/CGMSportFinance.sln

# Run API server (Swagger: http://localhost:5180/swagger)
dotnet run --project backend/src/CGMSportFinance.Api

# Run integration tests
dotnet test backend/CGMSportFinance.sln

# Frontend dev server
cd frontend && pnpm install && pnpm dev:antd

# Frontend type check
cd frontend && pnpm check:type

# Dev mode hot-reload
dotnet watch --project backend/src/CGMSportFinance.Api/CGMSportFinance.Api.csproj
```

No unit test projects at this time. Integration tests are in `backend/tests/CGMSportFinance.Api.IntegrationTests`.

---

## Pre-Task Requirements (Mandatory)

**BEFORE writing any code**, check if your task matches and read the required document:

| Task Keywords | MUST Read First |
|---------------|-----------------|
| Entity, Repository, Service, Controller, API, DbContext, Migration | `.claude/ARCHITECTURE.md` |
| Edit *.cs, Write *.cs, C#, class, method, async, LINQ, record | Auto-loaded (`~/.claude/rules/csharp-coding-style.md`) |
| EF Core, Migration, DbContext, Entity | `.claude/rules/code-first-ef.md` |
| Vue, Component, Pinia, TypeScript, Frontend | `.claude/rules/review-vue.md` (when available) |
| Review, Refactor, Quality | `.claude/LINUS_MODE.md` |

---

## Architecture (Summary)

Monorepo with backend API + frontend SPA. Full details in `.claude/ARCHITECTURE.md`.

**Key Patterns**: Features-based folders | JWT + HttpOnly Refresh Token | EF Core Code-First | API Envelope (`ApiEnvelope<T>`) | Encrypted Secrets

**Database**: PostgreSQL, EF Core Code-First. Migrations allowed via `dotnet ef`.

**Frontend**: Vue 3 Composition API + Ant Design Vue + Pinia + TypeScript strict mode

> Architecture details (project structure, auth flow, API patterns) -> see `.claude/ARCHITECTURE.md`

---

## Workflow (Automated)

The development workflow is **enforced by hooks**. All steps must be completed before commit:

```
Planner (optional) -> Code -> Simplifier -> Spec Check -> Code Review -> Security -> Linus -> Build -> Commit
```

### Required Steps

| Step | Tool/Agent | Completion Signal |
|------|------------|-------------------|
| 1. Code Simplifier | `code-simplifier:code-simplifier` agent | Say "simplifier done" |
| 1.5 Spec Check | Compare implementation vs requirements | Say "spec check done" |
| 2. Code Review | `code-review-specialist` agent | Say "code review done" |
| 3. Security Review | `security-vuln-scanner` agent | Say "security review done" |
| 4. Linus Review | Apply `.claude/LINUS_MODE.md` | Say "Linus Green" |
| 5. Build | Auto-verified on commit | Automatic |

### Agent Dispatch Rules (MANDATORY)

When workflow requires running agents (simplifier, code-review, security):
- You MUST use the Agent tool to dispatch. Do NOT substitute with your own text analysis.
- The `post-agent-verify` hook verifies Agent tool was actually called -- text summaries will NOT mark steps complete.
- Use `/commit-this` or `/review-workflow` slash commands for guided workflow execution.

### Workflow Commands

| Command | Description |
|---------|-------------|
| `workflow status` | View current workflow state and pending steps |
| `workflow reset` | Reset all workflow state (start fresh) |
| `workflow skip <step>` | Skip a specific step (not recommended) |
| `code-review` | Run full review workflow without commit |
| `commit this` | Run full workflow and create git commit |
| `/commit-this` | Full workflow with guided Agent dispatch + git commit |
| `/review-workflow` | Full review workflow without commit |

### Workflow Rules

- **Build FAIL** -> fix -> Build is automatically re-verified
- **Code file edits** automatically reset review steps
- Spec Check: re-read requirements, verify completeness, check YAGNI

### Tracked File Types

Code: `.cs`, `.razor`, `.json`, `.css`, `.js`, `.html`, `.csproj`, `.xml`, `.vue`, `.ts`, `.tsx`, `.mts`
Doc: `.md`, `.txt`, `.rst`, `.yml`, `.yaml` (skip build verification)

---

## Build & Verification

- After modifying .cs files -> `dotnet build --no-restore -v q`
- After modifying .vue/.ts/.tsx files -> `cd frontend && pnpm check:type`
- Build Self-Healing: max 5 retries, then stop and report (enforced by hook)
- DLL lock -> retry with `--no-incremental`, then tell user which process to close

### Related Skills
- `/commit-this` -- Execute full workflow and commit
- `/review-workflow` -- Execute review workflow without commit

---

## Coding Standards

Full guide: `~/.claude/rules/csharp-coding-style.md` (auto-loaded)

**Commits**: Conventional Commits 1.0.0, descriptions in Traditional Chinese (enforced by hook)

### Git

- **Staging**: Specific files only, never `git add .` or `-A` (enforced by hook)
- **Branch**: `feature/add-xxx`, `fix/xxx-error`, `refactor/xxx`
- **Merge**: Always `--no-ff` (reminded by hook)

### Forbidden

- `git add .` -- specific files only (hook blocked)
- Guessing -- use `[NEEDS CLARIFICATION]`
- Modifying encrypted secrets files directly
- Bypassing three-tier architecture

---

## Communication Style

See `.claude/rules/communication-style.md`
