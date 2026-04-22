# Review Vue — Vue 3 / TypeScript Review Checklist

本文件由 `Hans-OS/.claude/commands/review-vue.md` 鏡像整理，供 `Codex` 對前端變更進行人工 review。

## Critical

- 必須使用 Composition API + `<script setup lang="ts">`
- 不可缺少 `lang="ts"`
- composable 不可洩漏未妥善包裝的 reactive state
- `addEventListener` / `setInterval` / `setTimeout` 要在 `onUnmounted` 清理

## Important

- `defineProps()` 與 `defineEmits()` 要有型別
- 不可在元件內直接 inline API call，應透過 API layer
- 不可直接在元件外任意 mutate Pinia store state
- 若專案已配置 i18n，避免硬編 UI 字串
- `v-for` 必須有 `:key`

## Style

- Component file name 用 PascalCase
- script 內 prop 命名用 camelCase，template 內用 kebab-case
- 移除未使用 import
- 避免殘留 `console.log`
- 避免 template 中的 magic numbers

## Ant Design Vue Checks

- 元件引入風格要一致
- 表單驗證優先既有 form pattern
- Modal / Drawer 使用 `v-model:open`，避免舊 `visible`
- Table slot 寫法保持既有專案風格

## Pinia Checks

- store 命名採 `use*Store`
- async 操作放在 actions，不放 getter
- 元件解構 store state 時使用 `storeToRefs()`
- 避免 cross-store circular dependency

## Tailwind CSS Checks

- 能用 utility class 時避免 inline style
- spacing scale 保持一致
- responsive prefix 正確使用
- 若頁面已有 dark mode 機制，避免破壞其相容性

## Review Workflow

1. 找出本次修改的 `.vue` / `.ts` / `.tsx` 檔案
2. 逐檔閱讀與分類問題
3. 依 `Critical` / `Important` / `Style` 報告
4. 給出具體修正建議與路徑
