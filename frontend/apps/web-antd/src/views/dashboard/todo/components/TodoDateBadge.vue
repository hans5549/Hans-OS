<script setup lang="ts">
const props = defineProps<{
  dueDate: null | string | undefined;
}>();

const dateInfo = computed(() => {
  if (!props.dueDate) return null;

  const due = new Date(props.dueDate + 'T00:00:00');
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const tomorrow = new Date(today);
  tomorrow.setDate(today.getDate() + 1);

  const diffDays = Math.floor(
    (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24),
  );

  if (diffDays < 0) {
    return { label: `逾期 · ${formatDate(due)}`, color: 'red' };
  } else if (diffDays === 0) {
    return { label: '今天', color: 'orange' };
  } else if (diffDays === 1) {
    return { label: '明天', color: 'yellow' };
  } else if (diffDays <= 6) {
    return { label: formatWeekday(due), color: 'gray' };
  }
  return { label: formatDate(due), color: 'gray' };
});

const colorClass = computed(() => {
  const c = dateInfo.value?.color;
  if (c === 'red') return 'bg-red-100 text-red-600';
  if (c === 'orange') return 'bg-orange-100 text-orange-600';
  if (c === 'yellow') return 'bg-yellow-100 text-yellow-700';
  return 'bg-slate-100 text-slate-500';
});

function formatDate(date: Date) {
  return date.toLocaleDateString('zh-TW', { month: 'short', day: 'numeric' });
}

function formatWeekday(date: Date) {
  return date.toLocaleDateString('zh-TW', { weekday: 'short' });
}
</script>

<script lang="ts">
import { computed } from 'vue';
</script>

<template>
  <span
    v-if="dateInfo"
    class="inline-flex items-center rounded px-1.5 py-0.5 text-xs font-medium"
    :class="colorClass"
  >
    {{ dateInfo.label }}
  </span>
</template>
