---
description: 'Vue 3 frontend development guide'
---

# Vue 3 Frontend Development Guide

## Basic Rules

- **Must** use Composition API (`<script setup lang="ts">`)
- **Forbidden**: Options API
- **Must** use TypeScript strict mode
- **Must** include `lang="ts"` in `<script setup>`

## Component Guidelines

### Props and Emits

```vue
<!-- ✅ Correct — using TypeScript interface -->
<script setup lang="ts">
interface Props {
  title: string;
  count?: number;
}
const props = defineProps<Props>();

const emit = defineEmits<{
  update: [value: string];
  close: [];
}>();
</script>

<!-- ❌ Wrong — no type definitions -->
<script setup>
const props = defineProps(['title', 'count']);
</script>
```

### Component Naming

- File names use **PascalCase** (e.g., `UserProfile.vue`)
- Props use camelCase in script, kebab-case in template

## Pinia State Management

- Store files go in the `stores/` directory
- Naming convention: `use*Store` (e.g., `useAuthStore`)
- Use actions for async operations, not getters
- Use `storeToRefs()` when destructuring store state in components
- Cross-store circular dependencies are forbidden

## Ant Design Vue

- Globally registered components use the `a-` prefix, or use explicit imports — **be consistent**
- Prefer `<a-form>` with the `useForm` composable for forms
- `<a-table>` uses column slots with the `#bodyCell` pattern
- Modal/Drawer: use `v-model:open` (not the deprecated `visible`)

## Tailwind CSS

- Do not use inline `style` when Tailwind utility classes are available
- Use spacing scales consistently (do not mix arbitrary values)
- Use responsive prefixes correctly (`sm:`, `md:`, `lg:`)
- Support dark mode where applicable (`dark:` prefix)

## API Integration

- API calls go in the `src/api/` layer, not directly in components
- Use `RequestClient` configuration: `codeField: 'code'`, `dataField: 'data'`, `successCode: 0`
- `Authorization: Bearer <token>` is automatically injected
- Token is automatically refreshed on 401

## Important Notes

### Must Fix (Critical)

- Script setup missing `lang="ts"`
- Reactive objects returned from composables without using `toRefs()`
- `addEventListener`, `setInterval`, `setTimeout` not cleaned up in `onUnmounted`

### Should Fix (Important)

- `v-for` missing `:key` binding
- API calls made directly in components (should go through the API layer)
- Directly mutating Pinia store state (should use actions)

### Style Suggestions

- Unused imports
- Leftover `console.log`
- Magic numbers in templates
