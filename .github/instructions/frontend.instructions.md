---
applyTo:
  - 'frontend/**/*.vue'
  - 'frontend/**/*.ts'
  - 'frontend/**/*.tsx'
---

# Vue 3 Frontend Coding Rules

## Mandatory

- **Composition API** only: `<script setup lang="ts">` (Options API is forbidden)
- **TypeScript strict mode** — all files must pass `pnpm check:type`
- **Ant Design Vue** for UI components — do not build custom base components
- **Tailwind CSS** for utility styling
- **Pinia** for state management with `use*Store` naming

## Component Structure

```vue
<script setup lang="ts">
// 1. Imports
// 2. Props / Emits (defineProps, defineEmits)
// 3. Reactive state (ref, reactive, computed)
// 4. Lifecycle hooks
// 5. Methods
</script>

<template>
  <!-- Single root element preferred -->
</template>

<style scoped>
/* Scoped styles, prefer Tailwind classes in template */
</style>
```

## State Management

- Use Pinia stores in `frontend/apps/web-antd/src/store/`
- Naming: `useAuthStore`, `useMenuStore`, etc.
- Never mutate store state directly from components — use store actions

## API Integration

- API calls in `frontend/apps/web-antd/src/api/core/`
- Use `RequestClient` from `src/api/request.ts` (auto-injects Bearer token)
- API envelope: expect `{ code: 0, data: T }` format
- **Exception**: `POST /auth/refresh` returns raw string

## UI / Design Rules

- Use CSS Variables (`var(--xxx)`) for all colors — no hardcoded color values
- HSL format for new colors
- Support both light and dark mode for all new components
- Use Iconify format for icons (`"mdi:home"`, `"lucide:settings"`)
- Responsive: consider sidebar collapse, compact mode

## Forbidden

- Options API (`export default { ... }`)
- Simplified Chinese in comments (use Traditional Chinese `zh-TW`)
- `any` type (use proper TypeScript types)
- Direct DOM manipulation (use Vue reactivity)
