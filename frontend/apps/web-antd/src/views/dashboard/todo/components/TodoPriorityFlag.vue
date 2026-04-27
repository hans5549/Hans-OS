<script setup lang="ts">
import type { TodoPriority } from '#/api/core/todos';

import { computed } from 'vue';

import {
  getPriorityColor,
  getPriorityShortLabel,
  todoIcons,
} from '../composables/useTodoMeta';

const props = withDefaults(
  defineProps<{
    priority: TodoPriority;
    /** 'flag' = 旗幟圖示；'chip' = 文字徽章 */
    variant?: 'chip' | 'flag';
  }>(),
  { variant: 'flag' },
);

const color = computed(() => getPriorityColor(props.priority));
const label = computed(() => getPriorityShortLabel(props.priority));
const visible = computed(() => props.priority !== 'None');
</script>

<template>
  <span
    v-if="visible && variant === 'flag'"
    class="inline-flex size-5 items-center justify-center"
    :style="{ color }"
    :title="label"
  >
    <svg
      class="size-4"
      fill="currentColor"
      stroke="currentColor"
      stroke-linecap="round"
      stroke-linejoin="round"
      stroke-width="1.6"
      viewBox="0 0 24 24"
    >
      <path :d="todoIcons.flag" />
    </svg>
  </span>
  <span
    v-else-if="visible && variant === 'chip'"
    class="inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-semibold text-white"
    :style="{ backgroundColor: color }"
  >
    {{ label }}
  </span>
</template>
