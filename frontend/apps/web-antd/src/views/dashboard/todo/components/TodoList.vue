<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import TodoAddForm from './TodoAddForm.vue';
import TodoEmptyState from './TodoEmptyState.vue';
import TodoItemRow from './TodoItemRow.vue';

const store = useTodoStore();
const isAddFormOpen = ref(false);
const searchQuery = ref('');
let searchTimer: ReturnType<typeof setTimeout> | null = null;

// View title
const viewTitle = computed(() => {
  const view = store.currentView;
  if (view === 'today') return $t('page.todo.today');
  if (view === 'inbox') return $t('page.todo.inbox');
  if (view === 'upcoming') return $t('page.todo.upcoming');
  if (view === 'week') return $t('page.todo.week');
  if (view === 'all') return $t('page.todo.all');
  if (view === 'trash') return $t('page.todo.trash');
  if (view === 'search') return `搜尋：${store.searchQuery ?? ''}`;
  if (view === 'project' && store.currentProjectId) {
    return store.projects.find((p) => p.id === store.currentProjectId)?.name ?? $t('page.todo.project');
  }
  if (view === 'tag' && store.currentTagId) {
    return store.tags.find((t) => t.id === store.currentTagId)?.name ?? $t('page.todo.tag');
  }
  return $t('page.todo.today');
});

// Whether to show the add button
const canAdd = computed(() => store.currentView !== 'trash' && store.currentView !== 'search');

// Today progress bar
const showProgress = computed(() => store.currentView === 'today');
const progress = computed(() => {
  const { done, total } = store.todayProgress;
  if (total === 0) return 0;
  return Math.round((done / total) * 100);
});

// Items depending on view
const displayItems = computed(() => {
  if (store.currentView === 'trash') return store.trashItems;
  if (store.currentView === 'search') return store.searchResults;
  return store.items;
});

const pendingItems = computed(() => displayItems.value.filter((i) => i.status !== 'Done'));
const completedItems = computed(() => displayItems.value.filter((i) => i.status === 'Done'));

// Empty state config
const emptyConfig = computed(() => {
  const view = store.currentView;
  if (view === 'trash') {
    return { iconPath: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16', title: $t('page.todo.emptyTrash'), description: '' };
  }
  if (view === 'search') {
    return { iconPath: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z', title: '沒有搜尋結果', description: '' };
  }
  if (view === 'today' && store.todayProgress.done > 0 && store.todayProgress.total === store.todayProgress.done) {
    return { iconPath: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', title: $t('page.todo.emptyTodayDone'), description: '' };
  }
  if (view === 'today') return { iconPath: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z', title: $t('page.todo.emptyToday'), description: '' };
  if (view === 'inbox') return { iconPath: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z', title: $t('page.todo.emptyInbox'), description: '' };
  return { iconPath: 'M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7z', title: $t('page.todo.emptyProject'), description: '' };
});

function openAddForm() {
  if (!canAdd.value) return;
  isAddFormOpen.value = true;
}

function onSearchInput() {
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    const q = searchQuery.value.trim();
    if (q.length > 0) {
      store.search(q);
    } else if (store.currentView === 'search') {
      // Clear search — go back to default view
      store.setView('today');
      searchQuery.value = '';
    }
  }, 300);
}

function clearSearch() {
  searchQuery.value = '';
  if (store.currentView === 'search') {
    store.setView('today');
  }
}

// Sync search input when view changes away from search
watch(() => store.currentView, (v) => {
  if (v !== 'search') searchQuery.value = '';
});

onMounted(() => document.addEventListener('todo:add', openAddForm));
onUnmounted(() => {
  document.removeEventListener('todo:add', openAddForm);
  if (searchTimer) clearTimeout(searchTimer);
});
</script>

<template>
  <div class="flex h-full flex-1 flex-col overflow-hidden">
    <!-- Header -->
    <div class="border-b border-border bg-background px-6 py-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold text-foreground">{{ viewTitle }}</h1>
        <button
          v-if="canAdd"
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

      <!-- Search bar -->
      <div class="relative mt-3">
        <svg class="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
        </svg>
        <input
          v-model="searchQuery"
          class="w-full rounded-lg border border-border bg-muted py-1.5 pl-9 pr-8 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none"
          placeholder="搜尋任務..."
          type="search"
          @input="onSearchInput"
        />
        <button
          v-if="searchQuery"
          class="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          type="button"
          @click="clearSearch"
        >
          <svg class="size-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path d="M6 18L18 6M6 6l12 12" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
          </svg>
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

      <!-- Empty state -->
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

      <!-- Completed items (collapsible, hide in trash view) -->
      <div v-if="completedItems.length > 0 && store.currentView !== 'trash'" class="mt-4">
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

      <!-- Search results count -->
      <div v-if="store.currentView === 'search' && displayItems.length > 0" class="mt-2 text-xs text-muted-foreground">
        找到 {{ displayItems.length }} 筆結果
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
