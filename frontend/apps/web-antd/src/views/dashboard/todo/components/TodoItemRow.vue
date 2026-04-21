<script setup lang="ts">
import type { TodoItem } from '#/api/core/todos';

import { onUnmounted, ref } from 'vue';

import { useTodoStore } from '#/store/todo';

import TodoDateBadge from './TodoDateBadge.vue';
import TodoPriorityFlag from './TodoPriorityFlag.vue';

const props = defineProps<{
  item: TodoItem;
  isSelected: boolean;
}>();

const store = useTodoStore();
const isAnimating = ref(false);
let animationTimer: ReturnType<typeof setTimeout> | null = null;

async function handleToggle() {
  isAnimating.value = true;
  await store.toggleComplete(props.item.id);
  animationTimer = setTimeout(() => {
    isAnimating.value = false;
  }, 300);
}

onUnmounted(() => {
  if (animationTimer !== null) clearTimeout(animationTimer);
});

function handleRowClick() {
  store.selectItem(props.item.id);
}
</script>

<template>
  <div
    class="group flex cursor-pointer items-center gap-3 rounded-lg px-3 py-2 transition-colors duration-150"
    :class="[
      isSelected ? 'bg-primary/10' : 'hover:bg-accent/50',
      item.status === 'Done' ? 'opacity-50' : '',
    ]"
    @click="handleRowClick"
  >
    <!-- Checkbox -->
    <button
      class="relative flex size-5 flex-shrink-0 items-center justify-center rounded-full border-2 transition-all duration-150"
      :class="[
        item.status === 'Done'
          ? 'border-green-500 bg-green-500'
          : 'border-border hover:border-primary',
        isAnimating ? 'scale-75' : 'scale-100',
      ]"
      type="button"
      @click.stop="handleToggle"
    >
      <svg
        v-if="item.status === 'Done'"
        class="size-3 text-white"
        fill="none"
        stroke="currentColor"
        stroke-width="3"
        viewBox="0 0 24 24"
      >
        <path d="M5 13l4 4L19 7" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>

    <!-- Title -->
    <span
      class="flex-1 truncate text-sm text-foreground transition-all duration-200"
      :class="item.status === 'Done' ? 'line-through text-muted-foreground' : ''"
    >
      {{ item.title }}
    </span>

    <!-- Badges -->
    <div class="flex items-center gap-1.5">
      <TodoDateBadge :due-date="item.dueDate" />
      <TodoPriorityFlag :priority="item.priority" />
    </div>
  </div>
</template>
