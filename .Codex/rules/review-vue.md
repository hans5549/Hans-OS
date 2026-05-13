# Review Vue — Vue 3 / TypeScript Review Checklist

This file mirrors and organizes `Hans-OS/.claude/commands/review-vue.md` for manual `Codex` review of frontend changes.

## Critical

- Must use Composition API + `<script setup lang="ts">`.
- Must not omit `lang="ts"`.
- Composables must not leak improperly wrapped reactive state.
- `addEventListener` / `setInterval` / `setTimeout` must be cleaned up in `onUnmounted`.

## Important

- `defineProps()` and `defineEmits()` must be typed.
- Do not make inline API calls directly inside components; use the API layer.
- Do not arbitrarily mutate Pinia store state outside components.
- If the project has i18n configured, avoid hard-coded UI strings.
- `v-for` must have `:key`.

## Style

- Component filenames use PascalCase.
- Prop names use camelCase in scripts and kebab-case in templates.
- Remove unused imports.
- Avoid leftover `console.log`.
- Avoid magic numbers in templates.

## Ant Design Vue Checks

- Keep component import style consistent.
- Prefer existing form validation patterns.
- Use `v-model:open` for Modal / Drawer; avoid the legacy `visible`.
- Keep Table slot syntax consistent with the existing project style.

## Pinia Checks

- Store names use `use*Store`.
- Put async operations in actions, not getters.
- Use `storeToRefs()` when destructuring store state in components.
- Avoid cross-store circular dependencies.

## Tailwind CSS Checks

- Avoid inline style when a utility class can do the job.
- Keep the spacing scale consistent.
- Use responsive prefixes correctly.
- If the page already has a dark-mode mechanism, do not break its compatibility.

## Review Workflow

1. Identify changed `.vue` / `.ts` / `.tsx` files.
2. Read each file and classify issues.
3. Report by `Critical` / `Important` / `Style`.
4. Provide concrete fix suggestions and paths.
