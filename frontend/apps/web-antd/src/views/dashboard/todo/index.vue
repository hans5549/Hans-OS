<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';

import { useTabs } from '@vben/hooks';
import { useTabbarStore } from '@vben/stores';

import { useTodoStore } from '#/store/todo';

import TodoDetail from './components/TodoDetail.vue';
import TodoList from './components/TodoList.vue';
import TodoSidebar from './components/TodoSidebar.vue';

defineOptions({ name: 'TodoPage' });

const store = useTodoStore();
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
}

watch(() => route.query, syncViewFromQuery, { deep: true });

onMounted(async () => {
  // Remove stale Todo tabs from the old path-based routing (e.g. /todo/today, /todo/inbox)
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
onUnmounted(() => document.removeEventListener('keydown', handleKeydown));

function handleKeydown(e: KeyboardEvent) {
  const tag = (e.target as HTMLElement).tagName;
  if (['INPUT', 'TEXTAREA'].includes(tag)) return;
  if (e.key === 'n' || e.key === 'N') {
    document.dispatchEvent(new CustomEvent('todo:add'));
  }
  if (e.key === 'Escape') store.closeDetail();
}
</script>

<template>
  <div class="flex h-full overflow-hidden bg-background">
    <TodoSidebar />
    <TodoList class="flex-1" />
    <Transition name="slide-right">
      <div v-if="store.isDetailOpen && store.selectedItemDetail" class="w-80 flex-shrink-0">
        <TodoDetail :item="store.selectedItemDetail" @close="store.closeDetail" />
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.slide-right-enter-active,
.slide-right-leave-active {
  transition: transform 0.22s ease-out, opacity 0.22s ease-out;
}
.slide-right-enter-from,
.slide-right-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
