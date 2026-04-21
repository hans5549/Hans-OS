<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';

import { useTodoStore } from '#/store/todo';

import TodoDetail from './components/TodoDetail.vue';
import TodoList from './components/TodoList.vue';
import TodoSidebar from './components/TodoSidebar.vue';

defineOptions({ name: 'TodoPage' });

const store = useTodoStore();
const route = useRoute();

onMounted(async () => {
  await store.init();

  const path = route.path;
  if (path.includes('/todo/inbox')) store.setView('inbox');
  else if (path.includes('/todo/upcoming')) store.setView('upcoming');
  else if (path.includes('/todo/all')) store.setView('all');
  else if (path.includes('/todo/project/')) {
    const id = route.params['id'] as string;
    store.setView('project', id);
  }
});

function handleKeydown(e: KeyboardEvent) {
  const tag = (e.target as HTMLElement).tagName;
  const isInputFocused = ['INPUT', 'TEXTAREA'].includes(tag);
  if (!isInputFocused && e.key === 'n') {
    document.dispatchEvent(new CustomEvent('todo:add'));
  }
  if (e.key === 'Escape') {
    store.closeDetail();
  }
}

onMounted(() => document.addEventListener('keydown', handleKeydown));
onUnmounted(() => document.removeEventListener('keydown', handleKeydown));
</script>

<template>
  <div class="flex h-full overflow-hidden bg-slate-50">
    <TodoSidebar />
    <TodoList class="flex-1" />
    <Transition name="slide-right">
      <div v-if="store.isDetailOpen && store.selectedItem" class="w-80 flex-shrink-0">
        <TodoDetail :item="store.selectedItem" @close="store.closeDetail" />
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
