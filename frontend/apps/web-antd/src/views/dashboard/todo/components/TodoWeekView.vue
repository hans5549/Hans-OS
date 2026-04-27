<script setup lang="ts">
import type { TodoItem } from '#/api/core/todos';

import { computed } from 'vue';

import dayjs from 'dayjs';

import { useTodoStore } from '#/store/todo';

import TodoItemRow from './TodoItemRow.vue';

const store = useTodoStore();

const weekDays = computed(() => {
  // 從本週週日開始
  const today = dayjs();
  const start = today.startOf('week');
  return Array.from({ length: 7 }).map((_, i) => {
    const d = start.add(i, 'day');
    return {
      date: d.format('YYYY-MM-DD'),
      label: d.format('ddd'),
      dayNum: d.format('D'),
      isToday: d.isSame(today, 'day'),
    };
  });
});

function itemsForDay(date: string): TodoItem[] {
  return store.items.filter(
    (i: TodoItem) => i.scheduledDate === date || i.dueDate === date,
  );
}
</script>

<template>
  <main class="flex h-full min-w-0 flex-col overflow-hidden">
    <header
      class="flex flex-shrink-0 items-center gap-3 border-b border-border/60 px-6 py-3"
    >
      <h1 class="text-lg font-semibold text-foreground">本週</h1>
      <span class="text-sm text-muted-foreground">
        {{ weekDays[0]!.date }} ~ {{ weekDays[6]!.date }}
      </span>
    </header>

    <div class="flex flex-1 gap-2 overflow-x-auto p-3">
      <div
        v-for="day in weekDays"
        :key="day.date"
        class="flex min-w-[180px] flex-1 flex-col rounded-xl border bg-card/40"
        :class="
          day.isToday
            ? 'border-primary/60 shadow-sm'
            : 'border-border/60'
        "
      >
        <div
          class="flex items-baseline gap-2 border-b border-border/60 px-3 py-2"
          :class="day.isToday ? 'bg-primary/5' : ''"
        >
          <span
            class="text-xs font-semibold uppercase tracking-wide"
            :class="day.isToday ? 'text-primary' : 'text-muted-foreground'"
          >
            {{ day.label }}
          </span>
          <span
            class="text-base font-bold"
            :class="day.isToday ? 'text-primary' : 'text-foreground'"
          >
            {{ day.dayNum }}
          </span>
        </div>
        <div class="flex-1 overflow-y-auto p-1.5">
          <template v-if="itemsForDay(day.date).length === 0">
            <div
              class="flex h-full items-center justify-center px-2 py-4 text-xs text-muted-foreground"
            >
              無排程
            </div>
          </template>
          <TodoItemRow
            v-for="item in itemsForDay(day.date)"
            v-else
            :key="item.id"
            :item="item"
          />
        </div>
      </div>
    </div>
  </main>
</template>
