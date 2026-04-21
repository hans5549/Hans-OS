<script setup lang="ts">
import { computed, ref } from 'vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import TodoAddForm from './TodoAddForm.vue';
import TodoEmptyState from './TodoEmptyState.vue';
import TodoItemRow from './TodoItemRow.vue';

const store = useTodoStore();
const isAddFormOpen = ref(false);

// View title
const viewTitle = computed(() => {
  const view = store.currentView;
  if (view === 'today') return $t('page.todo.today');
  if (view === 'inbox') return $t('page.todo.inbox');
  if (view === 'upcoming') return $t('page.todo.upcoming');
  if (view === 'all') return $t('page.todo.all');
  if (view === 'project' && store.currentProjectId) {
    return store.projects.find((p) => p.id === store.currentProjectId)?.name ?? $t('page.todo.project');
  }
  return $t('page.todo.today');
});

// Today progress bar
const showProgress = computed(() => store.currentView === 'today');
const progress = computed(() => {
  const { done, total } = store.todayProgress;
  if (total === 0) return 0;
  return Math.round((done / total) * 100);
});

// Items split into pending and done
const pendingItems = computed(() => store.items.filter((i) => i.status !== 'Done'));
const completedItems = computed(() => store.items.filter((i) => i.status === 'Done'));

// Empty state config
const emptyConfig = computed(() => {
  const view = store.currentView;
  if (view === 'today' && store.todayProgress.done > 0 && store.todayProgress.total === store.todayProgress.done) {
    return { icon: '✅', title: $t('page.todo.emptyTodayDone'), description: '' };
  }
  if (view === 'today') return { icon: '📅', title: $t('page.todo.emptyToday'), description: '' };
  if (view === 'inbox') return { icon: '📭', title: $t('page.todo.emptyInbox'), description: '' };
  return { icon: '📁', title: $t('page.todo.emptyProject'), description: '' };
});

function openAddForm() {
  isAddFormOpen.value = true;
}
</script>

<template>
  <div class="flex h-full flex-1 flex-col overflow-hidden">
    <!-- Header -->
    <div class="border-b border-slate-200 bg-white px-6 py-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-slate-800">{{ viewTitle }}</h1>
        <button
          class="flex items-center gap-1.5 rounded-lg bg-blue-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-600"
          type="button"
          @click="openAddForm"
        >
          <svg class="size-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path d="M12 5v14m-7-7h14" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
          </svg>
          新增
        </button>
      </div>

      <!-- Today Progress Bar -->
      <div v-if="showProgress && store.todayProgress.total > 0" class="mt-3">
        <div class="mb-1 flex items-center justify-between text-xs text-slate-500">
          <span>{{ store.todayProgress.done }} / {{ store.todayProgress.total }} {{ $t('page.todo.completed') }}</span>
          <span>{{ progress }}%</span>
        </div>
        <div class="h-1.5 overflow-hidden rounded-full bg-slate-200">
          <div
            class="h-full rounded-full bg-blue-500 transition-all duration-500"
            :style="{ width: `${progress}%` }"
          />
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto bg-slate-50 p-4">
      <!-- Add Form (inline) -->
      <div v-if="isAddFormOpen" class="mb-3">
        <TodoAddForm
          :default-project-id="store.currentView === 'project' ? store.currentProjectId : null"
          @close="isAddFormOpen = false"
        />
      </div>

      <!-- Empty state (no pending items and form not open) -->
      <TodoEmptyState
        v-if="pendingItems.length === 0 && !isAddFormOpen"
        :icon="emptyConfig.icon"
        :title="emptyConfig.title"
        :description="emptyConfig.description"
      />

      <!-- Pending items -->
      <div
        v-else
        class="space-y-0.5 rounded-xl bg-white p-2 shadow-sm"
      >
        <TransitionGroup name="item">
          <TodoItemRow
            v-for="item in pendingItems"
            :key="item.id"
            :item="item"
            :is-selected="store.selectedItemId === item.id"
          />
        </TransitionGroup>
      </div>

      <!-- Completed items (collapsible) -->
      <div v-if="completedItems.length > 0" class="mt-4">
        <details class="group">
          <summary class="cursor-pointer select-none text-sm text-slate-500 hover:text-slate-700 mb-2">
            ✓ {{ completedItems.length }} {{ $t('page.todo.completed') }}
          </summary>
          <div class="space-y-0.5 rounded-xl bg-white p-2 shadow-sm">
            <TodoItemRow
              v-for="item in completedItems"
              :key="item.id"
              :item="item"
              :is-selected="store.selectedItemId === item.id"
            />
          </div>
        </details>
      </div>
    </div>
  </div>
</template>

<style scoped>
.item-enter-active,
.item-leave-active {
  transition: all 0.2s ease;
}
.item-enter-from,
.item-leave-to {
  opacity: 0;
  transform: translateX(-10px);
}
</style>
