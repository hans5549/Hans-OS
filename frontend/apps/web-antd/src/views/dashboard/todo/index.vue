<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';

import { useTabs } from '@vben/hooks';
import { useTabbarStore } from '@vben/stores';

import { useTodoStore } from '#/store/todo';

import { useTodoSelection } from './composables/useTodoSelection';
import TodoListPanel from './components/TodoListPanel.vue';
import TodoSidebar from './components/TodoSidebar.vue';
import TodoWeekView from './components/TodoWeekView.vue';

import './styles/todo-tokens.css';

defineOptions({ name: 'TodoPage' });

const store = useTodoStore();
const selection = useTodoSelection();
const route = useRoute();
const tabbarStore = useTabbarStore();
const { closeTabByKey } = useTabs();

function syncViewFromQuery() {
  const view = (route.query['view'] as string) ?? '';
  const id = (route.query['id'] as string) ?? '';
  if (view === 'inbox') store.setView('inbox');
  else if (view === 'upcoming') store.setView('upcoming');
  else if (view === 'all') store.setView('all');
  else if (view === 'trash') store.setView('trash');
  else if (view === 'week') store.setView('week');
  else if (view === 'project' && id) store.setView('project', id);
  else if (view === 'tag' && id) store.setView('tag', undefined, id);
  else store.setView('today');
  selection.clear();
}

watch(() => route.query, syncViewFromQuery, { deep: true });

function handleKeydown(e: KeyboardEvent) {
  const tag = (e.target as HTMLElement).tagName;
  if (['INPUT', 'TEXTAREA'].includes(tag)) return;
  if (e.key === 'n' || e.key === 'N') {
    e.preventDefault();
    document.dispatchEvent(new CustomEvent('todo:add'));
  }
  if (e.key === 'Escape') {
    if (selection.hasSelection.value) selection.clear();
    else store.closeDetail();
  }
}

onMounted(async () => {
  // 移除舊版以路徑為基礎的 stale tabs
  const staleTabs = tabbarStore.getTabs.filter(
    (tab) => tab.name === 'Todo' && tab.path !== '/todo',
  );
  for (const tab of staleTabs) {
    await closeTabByKey(tab.key as string);
  }

  await store.init();
  syncViewFromQuery();
  document.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown);
});
</script>

<template>
  <div class="flex h-full overflow-hidden bg-background">
    <TodoSidebar />
    <TodoWeekView v-if="store.currentView === 'week'" class="flex-1" />
    <TodoListPanel v-else class="flex-1" />
  </div>
</template>
