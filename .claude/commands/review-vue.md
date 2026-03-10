# Review Vue - Vue 3 Code Review

Scan specified files (or recently modified `.vue`, `.ts`, `.tsx` files) for Vue 3 best practices.

## Detection Rules

### Critical (must fix)
- **Options API usage**: Must use Composition API with `<script setup lang="ts">`
- **Missing TypeScript**: `<script setup>` without `lang="ts"`
- **Reactive state leak**: Returning reactive objects without `toRefs()` from composables
- **Missing cleanup**: `addEventListener`, `setInterval`, `setTimeout` without `onUnmounted` cleanup

### Important (should fix)
- **Non-typed props**: `defineProps()` without TypeScript interface
- **Non-typed emits**: `defineEmits()` without TypeScript event signatures
- **Direct store mutation**: Mutating Pinia store state outside of actions
- **Inline API calls**: API calls directly in components instead of through API layer
- **Hardcoded strings**: UI text not using i18n (if i18n is configured)
- **Missing `key` on `v-for`**: List rendering without `:key` binding

### Style (nice to have)
- **Component naming**: Must use PascalCase for component file names
- **Prop naming**: Must use camelCase in script, kebab-case in template
- **Unused imports**: Imported but never referenced
- **Console.log**: Debug statements left in code
- **Magic numbers in template**: Numeric literals without named constants
- **Ant Design Vue consistency**: Mixed component import styles

## Ant Design Vue Checks
- Use `a-` prefix components from global registration, OR explicit imports — be consistent
- Prefer `<a-form>` with `useForm` composable over manual validation
- Use `<a-table>` column slots correctly with `#bodyCell` pattern
- Modal/Drawer: use `v-model:open` (not deprecated `visible`)

## Pinia Store Checks
- Store files in `stores/` directory with `use*Store` naming
- Actions for async operations, not getters
- `storeToRefs()` when destructuring store state in components
- No cross-store circular dependencies

## Tailwind CSS Checks
- No inline `style` when Tailwind utility exists
- Consistent spacing scale usage (not mixing arbitrary values)
- Responsive prefixes used correctly (`sm:`, `md:`, `lg:`)
- Dark mode support if applicable (`dark:` prefix)

## Workflow

1. Identify target files (argument or recently modified `.vue`/`.ts`/`.tsx` from `git diff --name-only`)
2. Read each file and scan for patterns above
3. Classify findings by severity: Critical / Important / Style
4. Report findings with file:line references
5. Offer to fix issues one-by-one with user confirmation
6. Track progress: `[x/total] fixed`

## Usage

```
/review-vue                           # Scan recently modified Vue/TS files
/review-vue path/to/Component.vue     # Scan specific file
/review-vue --all                     # Scan all .vue files in frontend/
```

## Output Format

```
## Vue Review Report: ComponentName.vue

### Critical
- line 1: Missing `lang="ts"` in script setup

### Important
- line 15: Props defined without TypeScript interface
- line 42: API call directly in component — move to API layer

### Style
- line 8: Unused import `ref` from 'vue'

### Ant Design Vue
- line 30: Using deprecated `visible` prop — use `v-model:open`

Summary: 1 critical, 2 important, 1 style, 1 antd
```
