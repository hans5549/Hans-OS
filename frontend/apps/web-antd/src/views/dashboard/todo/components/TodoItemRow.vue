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

const difficultyConfig: Record<string, { color: string; label: string }> = {
  Easy: { color: '#22C55E', label: '易' },
  Hard: { color: '#EF4444', label: '難' },
  Medium: { color: '#F97316', label: '中' },
  None: { color: '', label: '' },
};

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

    <!-- Title + project badge -->
    <div class="flex min-w-0 flex-1 flex-col gap-0.5">
      <span
        class="truncate text-sm text-foreground transition-all duration-200"
        :class="item.status === 'Done' ? 'line-through text-muted-foreground' : ''"
      >
        {{ item.title }}
      </span>
      <!-- Project badge (shown when viewing all/search) -->
      <div
        v-if="item.projectName && (store.currentView === 'all' || store.currentView === 'search')"
        class="flex items-center gap-1"
      >
        <span
          class="size-2 rounded-full flex-shrink-0"
          :style="{ backgroundColor: item.projectColor ?? '#94A3B8' }"
        />
        <span class="truncate text-xs text-muted-foreground">{{ item.projectName }}</span>
      </div>
    </div>

    <!-- Badges -->
    <div class="flex items-center gap-1.5 flex-shrink-0">
      <!-- Tags (max 2) -->
      <span
        v-for="tag in item.tags.slice(0, 2)"
        :key="tag.id"
        class="rounded-full px-1.5 py-0.5 text-xs text-white"
        :style="{ backgroundColor: tag.color ?? '#64748B' }"
      >
        {{ tag.name }}
      </span>
      <span
        v-if="item.tags.length > 2"
        class="rounded-full bg-accent px-1.5 py-0.5 text-xs text-muted-foreground"
      >
        +{{ item.tags.length - 2 }}
      </span>

      <!-- Difficulty badge -->
      <span
        v-if="item.difficulty !== 'None'"
        class="rounded px-1 py-0.5 text-xs text-white font-medium"
        :style="{ backgroundColor: difficultyConfig[item.difficulty]?.color ?? '#94A3B8' }"
      >
        {{ difficultyConfig[item.difficulty]?.label }}
      </span>

      <TodoDateBadge :due-date="item.dueDate" />
      <TodoPriorityFlag :priority="item.priority" />
    </div>
  </div>
</template>
