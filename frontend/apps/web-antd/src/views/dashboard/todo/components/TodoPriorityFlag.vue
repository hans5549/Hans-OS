<script setup lang="ts">
import type { TodoPriority } from '#/api/core/todos';

const props = defineProps<{
  priority: TodoPriority;
  size?: 'sm' | 'md';
}>();

const priorityConfig: Record<TodoPriority, { color: string; label: string }> = {
  High: { color: '#EF4444', label: 'P1' },
  Medium: { color: '#F97316', label: 'P2' },
  Low: { color: '#3B82F6', label: 'P3' },
  None: { color: '#94A3B8', label: 'P4' },
};

const config = computed(() => priorityConfig[props.priority]);
</script>

<script lang="ts">
import { computed } from 'vue';
</script>

<template>
  <span
    v-if="priority !== 'None'"
    class="inline-flex items-center rounded px-1 text-xs font-medium text-white"
    :style="{ backgroundColor: config.color }"
  >
    {{ config.label }}
  </span>
</template>
