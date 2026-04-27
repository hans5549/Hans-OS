<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  dueDate: null | string | undefined;
}>();

interface DateInfo {
  label: string;
  variant: 'future' | 'overdue' | 'today' | 'tomorrow';
}

const dateInfo = computed<DateInfo | null>(() => {
  if (!props.dueDate) return null;

  const due = new Date(props.dueDate + 'T00:00:00');
  if (Number.isNaN(due.getTime())) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const diffDays = Math.floor(
    (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24),
  );

  if (diffDays < 0) {
    return { label: `逾期 · ${formatDate(due)}`, variant: 'overdue' };
  }
  if (diffDays === 0) return { label: '今天', variant: 'today' };
  if (diffDays === 1) return { label: '明天', variant: 'tomorrow' };
  if (diffDays <= 6) return { label: formatWeekday(due), variant: 'future' };
  return { label: formatDate(due), variant: 'future' };
});

function formatDate(date: Date) {
  return date.toLocaleDateString('zh-TW', { day: 'numeric', month: 'short' });
}

function formatWeekday(date: Date) {
  return date.toLocaleDateString('zh-TW', { weekday: 'short' });
}
</script>

<template>
  <span
    v-if="dateInfo"
    class="inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-[11px] font-medium"
    :data-variant="dateInfo.variant"
  >
    <svg
      class="size-3"
      fill="none"
      stroke="currentColor"
      stroke-linecap="round"
      stroke-linejoin="round"
      stroke-width="2"
      viewBox="0 0 24 24"
    >
      <path d="M8 2v4 M16 2v4 M3 10h18" />
      <path
        d="M21 18V8a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v10a4 4 0 0 0 4 4h10a4 4 0 0 0 4-4z"
      />
    </svg>
    {{ dateInfo.label }}
  </span>
</template>

<style scoped>
span[data-variant='overdue'] {
  background: rgb(var(--todo-date-overdue-bg));
  color: rgb(var(--todo-date-overdue-fg));
}
span[data-variant='today'] {
  background: rgb(var(--todo-date-today-bg));
  color: rgb(var(--todo-date-today-fg));
}
span[data-variant='tomorrow'] {
  background: rgb(var(--todo-date-tomorrow-bg));
  color: rgb(var(--todo-date-tomorrow-fg));
}
span[data-variant='future'] {
  background: rgb(var(--todo-date-future-bg));
  color: rgb(var(--todo-date-future-fg));
}
</style>
