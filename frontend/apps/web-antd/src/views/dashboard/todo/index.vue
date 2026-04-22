<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';

import { useTodoStore } from '#/store/todo';

import TodoDetail from './components/TodoDetail.vue';
import TodoList from './components/TodoList.vue';
import TodoSidebar from './components/TodoSidebar.vue';

defineOptions({ name: 'TodoPage' });

const store = useTodoStore();
const route = useRoute();

function syncViewFromRoute(path: string) {
  if (path.includes('/todo/inbox')) store.setView('inbox');
  else if (path.includes('/todo/upcoming')) store.setView('upcoming');
  else if (path.includes('/todo/all')) store.setView('all');
  else if (path.includes('/todo/trash')) store.setView('trash');
  else if (path.includes('/todo/week')) store.setView('week');
  else if (path.match(/\/todo\/project\/(.+)/)) {
    const id = route.params['id'] as string;
    store.setView('project', id);
  } else if (path.match(/\/todo\/tag\/(.+)/)) {
    const id = route.params['id'] as string;
    store.setView('tag', undefined, id);
  } else {
    store.setView('today');
  }
}

watch(() => route.fullPath, syncViewFromRoute);

onMounted(async () => {
  await store.init();
  syncViewFromRoute(route.path);
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
