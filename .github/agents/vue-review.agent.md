---
name: vue-review
description: Vue 3 Composition API + Ant Design Vue + TypeScript 最佳實踐審查（只讀）
tools: ["read", "search"]
---

# Vue 3 Review Agent

Scans specified files (or recently modified `.vue`, `.ts`, `.tsx` files) to check Vue 3 best practices.

## Detection Rules

### Critical (Must Fix)

- **Options API Usage**: Must use Composition API (`<script setup lang="ts">`)
- **Missing TypeScript**: `<script setup>` missing `lang="ts"`
- **Reactive State Leaking**: Returning reactive objects from composables without using `toRefs()`
- **Missing Cleanup**: `addEventListener`, `setInterval`, `setTimeout` not cleaned up in `onUnmounted`

### Important (Should Fix)

- **Untyped Props**: `defineProps()` missing TypeScript interface
- **Untyped Emits**: `defineEmits()` missing TypeScript event signatures
- **Direct Store Mutation**: Mutating Pinia store state directly outside of actions
- **Inline API Calls**: Calling APIs directly in components instead of going through the API layer
- **`v-for` Missing `:key`**: List rendering missing `:key` binding

### Style (Suggested Improvement)

- **Component Naming**: Must use PascalCase file names
- **Prop Naming**: Use camelCase in script, kebab-case in template
- **Unused Imports**: Imported but not referenced
- **Console.log**: Leftover debug statements
- **Magic numbers in templates**: Unnamed numeric constants

## Ant Design Vue Checks

- Use globally registered components with `a-` prefix, or explicit imports — be consistent
- Prefer `<a-form>` with `useForm` composable for forms
- `<a-table>` uses column slots with the `#bodyCell` pattern
- Modal/Drawer: Use `v-model:open` (not the deprecated `visible`)

## Pinia Store Checks

- Store files placed in `stores/` directory with `use*Store` naming
- Use actions for async operations, not getters
- Use `storeToRefs()` when destructuring in components
- No circular dependencies across stores

## Tailwind CSS Checks

- Do not use inline `style` when Tailwind utility classes are available
- Use spacing scale consistently (do not mix arbitrary values)
- Use responsive prefixes correctly (`sm:`, `md:`, `lg:`)
- Support dark mode (`dark:` prefix)

## Workflow

1. Identify target files (arguments or recently modified `.vue`/`.ts`/`.tsx`, obtained from `git diff --name-only`)
2. Read each file and scan for the patterns above
3. Classify by severity: Critical / Important / Style
4. Report findings with file:line references
5. Provide fix suggestions

## Output Format

```
## Vue Review Report: ComponentName.vue

### Critical
- Line 1: script setup missing `lang="ts"`

### Important
- Line 15: Props definition missing TypeScript interface
- Line 42: API called directly in component — move to API layer

### Style
- Line 8: Unused import `ref` from 'vue'

### Ant Design Vue
- Line 30: Using deprecated `visible` prop — use `v-model:open`

Summary: 1 critical, 2 important, 1 style, 1 antd
```

## Hans-OS Frontend Context

- **Main App**: `frontend/apps/web-antd`
- **Shared Packages**: `frontend/packages/`
- **Tech Stack**: Vue 3 + Ant Design Vue + TypeScript strict + Pinia + Tailwind CSS
- **API Integration**: `src/api/request.ts` — `codeField: 'code'`, `dataField: 'data'`, `successCode: 0`
