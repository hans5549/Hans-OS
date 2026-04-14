<script setup lang="ts">
import { $t } from '#/locales';
import { computed, ref } from 'vue';

interface TodoItem {
  dueAt: string;
  priority: 'high' | 'low' | 'medium';
  status: 'done' | 'inProgress' | 'pending';
  summaryKey: string;
  titleKey: string;
}

defineOptions({ name: 'TodoPage' });

const todoItems = ref<TodoItem[]>([
  {
    dueAt: '2026-04-15',
    priority: 'high',
    status: 'inProgress',
    summaryKey: 'page.dashboard.todoSummaryContract',
    titleKey: 'page.dashboard.todoTitleContract',
  },
  {
    dueAt: '2026-04-16',
    priority: 'medium',
    status: 'pending',
    summaryKey: 'page.dashboard.todoSummaryData',
    titleKey: 'page.dashboard.todoTitleData',
  },
  {
    dueAt: '2026-04-18',
    priority: 'high',
    status: 'pending',
    summaryKey: 'page.dashboard.todoSummarySmoke',
    titleKey: 'page.dashboard.todoTitleSmoke',
  },
  {
    dueAt: '2026-04-20',
    priority: 'low',
    status: 'done',
    summaryKey: 'page.dashboard.todoSummaryScope',
    titleKey: 'page.dashboard.todoTitleScope',
  },
]);

const completedCount = computed(
  () => todoItems.value.filter((item) => item.status === 'done').length,
);

const inProgressCount = computed(
  () => todoItems.value.filter((item) => item.status === 'inProgress').length,
);

const pendingCount = computed(
  () => todoItems.value.filter((item) => item.status === 'pending').length,
);

const priorityLabelKeyMap: Record<TodoItem['priority'], string> = {
  high: 'page.dashboard.todoPriorityHigh',
  low: 'page.dashboard.todoPriorityLow',
  medium: 'page.dashboard.todoPriorityMedium',
};

const statusLabelKeyMap: Record<TodoItem['status'], string> = {
  done: 'page.dashboard.todoStatusDone',
  inProgress: 'page.dashboard.todoStatusInProgress',
  pending: 'page.dashboard.todoStatusPending',
};

function getPriorityClass(priority: TodoItem['priority']) {
  switch (priority) {
    case 'high':
      return 'bg-red-500/12 text-red-600 dark:text-red-300';
    case 'medium':
      return 'bg-amber-500/12 text-amber-600 dark:text-amber-300';
    default:
      return 'bg-slate-500/12 text-slate-600 dark:text-slate-300';
  }
}

function getStatusClass(status: TodoItem['status']) {
  switch (status) {
    case 'done':
      return 'bg-emerald-500/12 text-emerald-600 dark:text-emerald-300';
    case 'inProgress':
      return 'bg-blue-500/12 text-blue-600 dark:text-blue-300';
    default:
      return 'bg-purple-500/12 text-purple-600 dark:text-purple-300';
  }
}

function getPriorityLabel(priority: TodoItem['priority']) {
  return $t(priorityLabelKeyMap[priority]);
}

function getStatusLabel(status: TodoItem['status']) {
  return $t(statusLabelKeyMap[status]);
}
</script>

<template>
  <div class="p-5 space-y-5">
    <section class="rounded-2xl border border-border bg-card p-6 text-card-foreground">
      <div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
        <div class="space-y-2">
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-semibold tracking-tight">{{ $t('page.dashboard.todo') }}</h1>
            <span
              class="rounded-full bg-primary/12 px-2.5 py-1 text-xs font-medium text-primary"
            >
              {{ $t('page.dashboard.todoDemo') }}
            </span>
          </div>
          <p class="max-w-2xl text-sm text-muted-foreground">
            {{ $t('page.dashboard.todoDescription') }}
          </p>
        </div>
        <div class="rounded-xl border border-dashed border-border px-4 py-3 text-sm text-muted-foreground">
          {{ $t('page.dashboard.todoDeploymentHint') }}
        </div>
      </div>
    </section>

    <section class="grid gap-4 md:grid-cols-3">
      <article class="rounded-2xl border border-border bg-card p-5 text-card-foreground">
        <p class="text-sm text-muted-foreground">{{ $t('page.dashboard.todoStatusPending') }}</p>
        <p class="mt-2 text-3xl font-semibold">{{ pendingCount }}</p>
      </article>
      <article class="rounded-2xl border border-border bg-card p-5 text-card-foreground">
        <p class="text-sm text-muted-foreground">{{ $t('page.dashboard.todoStatusInProgress') }}</p>
        <p class="mt-2 text-3xl font-semibold">{{ inProgressCount }}</p>
      </article>
      <article class="rounded-2xl border border-border bg-card p-5 text-card-foreground">
        <p class="text-sm text-muted-foreground">{{ $t('page.dashboard.todoStatusDone') }}</p>
        <p class="mt-2 text-3xl font-semibold">{{ completedCount }}</p>
      </article>
    </section>

    <section class="rounded-2xl border border-border bg-card text-card-foreground">
      <header class="border-b border-border px-6 py-4">
        <h2 class="text-lg font-semibold">{{ $t('page.dashboard.todoListTitle') }}</h2>
      </header>
      <ul class="divide-y divide-border">
        <li
          v-for="item in todoItems"
          :key="item.titleKey"
          class="flex flex-col gap-4 px-6 py-5 lg:flex-row lg:items-start lg:justify-between"
        >
          <div class="space-y-3">
            <div class="flex flex-wrap items-center gap-2">
              <h3 class="text-base font-semibold">{{ $t(item.titleKey) }}</h3>
              <span
                :class="getStatusClass(item.status)"
                class="rounded-full px-2.5 py-1 text-xs font-medium"
              >
                {{ getStatusLabel(item.status) }}
              </span>
              <span
                :class="getPriorityClass(item.priority)"
                class="rounded-full px-2.5 py-1 text-xs font-medium"
              >
                {{ getPriorityLabel(item.priority) }}
              </span>
            </div>
            <p class="max-w-3xl text-sm leading-6 text-muted-foreground">
              {{ $t(item.summaryKey) }}
            </p>
          </div>
          <div class="shrink-0 text-sm text-muted-foreground">
            {{ $t('page.dashboard.todoDueAt') }}{{ item.dueAt }}
          </div>
        </li>
      </ul>
    </section>
  </div>
</template>
