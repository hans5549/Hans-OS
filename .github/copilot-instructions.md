# Hans-OS Copilot Instructions

## Build, run, and verification

- Build the backend with `dotnet build backend/HansOS.slnx`.
- Run the API server with `dotnet run --project backend/src/HansOS.Api` (Swagger: http://localhost:5180/swagger).
- Run all backend tests with `dotnet test backend/HansOS.slnx`.
- Run the frontend dev server with `cd frontend && pnpm dev:antd` (port 5666).
- Run frontend type check with `cd frontend && pnpm check:type`.
- There is no standalone lint command documented for contributors; do not invent one.

## High-level architecture

- This is a monorepo with a .NET 9.0 backend API and a Vue 3 frontend SPA.
- **Backend** (`backend/`): ASP.NET Core Web API with three-layer architecture (Controller → Service → DbContext).
  - Solution: `backend/HansOS.slnx`
  - Main project: `backend/src/HansOS.Api`
  - Unit tests: `backend/tests/HansOS.Api.UnitTests`
  - Integration tests: `backend/tests/HansOS.Api.IntegrationTests`
- **Frontend** (`frontend/`): Vue Vben Admin monorepo (pnpm + Turborepo).
  - Main app: `frontend/apps/web-antd` (Vue 3 + Ant Design Vue + TypeScript strict mode)
  - Shared packages: `frontend/packages/` (@core, stores, effects)
- **Database**: PostgreSQL with EF Core Code-First. Migrations auto-applied on startup.
- **Authentication**: JWT bearer tokens + HttpOnly refresh token cookie.
- API responses wrapped in `ApiEnvelope<T>` with `{ code: 0, data, error, message }` format.

## Key conventions

- Reply to the user in Traditional Chinese (`zh-TW`) unless they explicitly ask for another language.
- Start with `CLAUDE.md` for verified build/run commands and workflow expectations.
- Read `.claude/ARCHITECTURE.md` before changing entities, services, controllers, APIs, or auth logic.
- This is a Code-First EF Core codebase. Add migrations with `dotnet ef migrations add <Name> --project backend/src/HansOS.Api`.
- Pages and controllers should call services, not repositories or DbContext directly.
- Frontend uses Vue 3 Composition API with `<script setup lang="ts">`. Options API is forbidden.
- Use Pinia for state management with `use*Store` naming convention.
- Ant Design Vue components for UI. Tailwind CSS for utility styling.
- Commit messages follow Conventional Commits with Traditional Chinese descriptions.
- Every new API endpoint and public method must have corresponding tests.
