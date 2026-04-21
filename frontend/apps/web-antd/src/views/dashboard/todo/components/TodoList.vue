<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';

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
    return { iconPath: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', title: $t('page.todo.emptyTodayDone'), description: '' };
  }
  if (view === 'today') return { iconPath: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z', title: $t('page.todo.emptyToday'), description: '' };
  if (view === 'inbox') return { iconPath: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z', title: $t('page.todo.emptyInbox'), description: '' };
  return { iconPath: 'M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7z', title: $t('page.todo.emptyProject'), description: '' };
});

function openAddForm() {
  isAddFormOpen.value = true;
}

onMounted(() => document.addEventListener('todo:add', openAddForm));
onUnmounted(() => document.removeEventListener('todo:add', openAddForm));
</script>

<template>
  <div class="flex h-full flex-1 flex-col overflow-hidden">
    <!-- Header -->
    <div class="border-b border-border bg-background px-6 py-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-foreground">{{ viewTitle }}</h1>
        <button
          class="flex items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary-hover"
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
        <div class="mb-1 flex items-center justify-between text-xs text-muted-foreground">
          <span>{{ store.todayProgress.done }} / {{ store.todayProgress.total }} {{ $t('page.todo.completed') }}</span>
          <span>{{ progress }}%</span>
        </div>
        <div class="h-1.5 overflow-hidden rounded-full bg-border">
          <div
            class="h-full rounded-full bg-primary transition-all duration-500 tech-glow"
            :style="{ width: `${progress}%` }"
          />
        </div>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto bg-background p-4">
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
        :icon-path="emptyConfig.iconPath"
        :title="emptyConfig.title"
        :description="emptyConfig.description"
      />

      <!-- Pending items -->
      <div
        v-else
        class="glass-card space-y-0.5 p-2"
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
          <summary class="mb-2 cursor-pointer select-none text-sm text-muted-foreground hover:text-foreground">
            ✓ {{ completedItems.length }} {{ $t('page.todo.completed') }}
          </summary>
          <div class="glass-card space-y-0.5 p-2">
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
