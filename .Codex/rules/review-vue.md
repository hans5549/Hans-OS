# Review Vue - Vue 3 / TypeScript Checklist

Use this for frontend changes under `frontend/apps/web-antd` and shared Vben packages.

## Current Project Shape

- Main app: `frontend/apps/web-antd`
- API wrappers: `frontend/apps/web-antd/src/api/core/*`
- Request client: `frontend/apps/web-antd/src/api/request.ts`
- Auth store: `frontend/apps/web-antd/src/store/auth.ts`
- Routes and guards: `frontend/apps/web-antd/src/router/*`
- Views: `frontend/apps/web-antd/src/views/*`
- Typecheck command: `cd frontend && pnpm check:type`

## Critical

- Use Composition API with `<script setup lang="ts">`.
- Do not omit `lang="ts"`.
- Do not use Options API for new code.
- Do not use `any` when a proper type can be expressed.
- Composables must not leak improperly wrapped reactive state.
- `addEventListener`, `setInterval`, `setTimeout`, observers, and subscriptions must be cleaned up in `onUnmounted`.
- API calls must go through `src/api/core/*` wrappers, not inline component calls.
- Preserve the `ApiEnvelope` client contract: `codeField = "code"`, `dataField = "data"`, `successCode = 0`.

## Important

- Type `defineProps()` and `defineEmits()`.
- Use Pinia actions for async store work; do not mutate store state directly from components.
- Use `storeToRefs()` when destructuring reactive store state.
- `v-for` must have stable `:key`.
- Preserve backend-driven menu and route behavior through `getAllMenusApi()` and `generateAccessible()`.
- Preserve auth redirect behavior in `router/guard.ts`.
- Keep refresh-token behavior aligned with `baseRequestClient` for `/auth/refresh` and `/auth/logout`.
- New user-visible text should be Traditional Chinese unless the surrounding Vben source is intentionally upstream text.

## Ant Design Vue

- Prefer Ant Design Vue components for base UI.
- Keep existing component import style.
- Prefer existing form validation patterns.
- Use `v-model:open` for Modal / Drawer; avoid legacy `visible`.
- Keep Table slot syntax consistent with current project style, especially `#bodyCell`.
- Use Ant Design message/notification patterns already present in the app.

## Pinia

- Store names use `use*Store`.
- Put async operations in actions, not getters.
- Avoid cross-store circular dependencies.
- Keep auth/access/user responsibilities separated.

## Tailwind and Styling

- Prefer Tailwind utility classes for spacing and layout.
- Use CSS variables / Vben design tokens for colors.
- Support dark mode where applicable.
- Avoid inline style when utility classes or scoped CSS are clearer.
- Keep responsive behavior for sidebar collapse, compact mode, and smaller viewports.

## API Integration

- Add or update API wrapper types in `src/api/core/*`.
- Use `requestClient` for standard enveloped endpoints.
- Use `baseRequestClient` only for endpoints that intentionally do not return the standard envelope.
- Do not duplicate endpoint strings across components.
- Align request/response DTOs with backend `Models/*`.

## Style

- Component filenames use PascalCase for reusable components.
- Route page `index.vue` files are acceptable where the existing folder pattern uses them.
- Prop names use camelCase in scripts and kebab-case in templates.
- Remove unused imports and leftover `console.log`.
- Avoid magic numbers in templates.
- Avoid one-off base UI components when Ant Design Vue already solves the problem.

## Review Workflow

1. Identify changed `.vue`, `.ts`, and `.tsx` files.
2. Read related API wrapper, store, route, and backend endpoint contracts if touched.
3. Classify findings by Critical / Important / Style.
4. Provide concrete fixes with file paths and line references.
5. Verify with `cd frontend && pnpm check:type`.
