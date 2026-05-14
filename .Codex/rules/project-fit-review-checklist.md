# Project Fit Review Checklist - Hans-OS

Use this with `code-review-specialist` and broader quality reviews. The mission is to verify whether a change fits Hans-OS architecture and current contracts, not to run a generic style pass.

## Backend Architecture

- Preserve Controller -> Service -> DbContext direction.
- Controllers handle HTTP surface, auth attributes, model binding, and envelope return shape.
- Services own business rules, validation, permission checks, transactions, import/export coordination, and persistence orchestration.
- Do not move business logic into controllers or Vue components.
- New cross-module helpers must solve current duplication or complexity; single-use helpers should stay local.

## API Contract and Auth

- Standard endpoints return `ApiEnvelope<T>.Success(...)` or `ApiEnvelope<T>.Fail(...)`.
- Keep the known `POST /auth/refresh` raw-string exception aligned with frontend `baseRequestClient`.
- Actions should explicitly communicate auth intent through `[Authorize]` or `[AllowAnonymous]` when editing touched controllers.
- User identity must come from claims, not request bodies.
- Preserve JWT access token plus HttpOnly refresh cookie behavior.
- Do not hard-code signing keys, connection strings, passwords, or real secrets.
- CORS with credentials must remain compatible with the refresh cookie flow.

## EF Core and Data

- Use EF Core Code-First and keep the migration chain intact.
- Prefer Fluent API configuration under `Data/Configurations/<Domain>/`.
- Review generated migrations and `ApplicationDbContextModelSnapshot`.
- Read-only queries should use `AsNoTracking()`.
- Prefer projection with `Select()` for response shapes.
- Avoid N+1 queries and unbounded list loads; add pagination for lists that can grow.
- Define decimal precision explicitly for money-like values.
- Pass `CancellationToken` through EF async calls.

## Menu, Routes, and Permissions

- Preserve backend-driven menu and route generation through `getAllMenusApi()` and `generateAccessible()`.
- Menu APIs must not leak unauthorized menu records.
- Permission codes should stay centralized instead of scattered as string literals in business logic.
- Frontend visibility checks are secondary protection; server-side authorization must still be correct.

## Frontend Fit

- Use Vue 3 Composition API with `<script setup lang="ts">`.
- Use API wrappers under `frontend/apps/web-antd/src/api/core/*`; do not call backend endpoints inline from components.
- Preserve the Vben request client contract: `codeField`, `dataField`, `successCode`, bearer injection, language header, and 401 refresh handling.
- Keep auth/access/user state responsibilities separated in Pinia stores.
- Prefer Ant Design Vue and existing Vben components for base UI.
- Keep route modules, page components, and shared packages aligned with existing folder patterns.
- Support dark mode and compact/sidebar states when changing UI surfaces.

## Spec and Scope

- Compare implementation against the user request or accepted plan.
- Flag missing requirements, behavior changes outside scope, and speculative features.
- If the implementation intentionally deviates from the plan, require an explicit reason.
- Verification must match the touched surface: backend build/tests for backend, `pnpm check:type` for frontend, both for cross-stack changes.
