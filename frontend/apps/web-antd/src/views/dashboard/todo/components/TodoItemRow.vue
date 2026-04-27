<script setup lang="ts">
import type { TodoItem } from '#/api/core/todos';

import { computed, onUnmounted, ref } from 'vue';

import { useTodoStore } from '#/store/todo';

import { useTodoSelection } from '../composables/useTodoSelection';
import {
  getDifficultyColor,
  getDifficultyShortLabel,
  todoIcons,
} from '../composables/useTodoMeta';
import TodoDateBadge from './TodoDateBadge.vue';
import TodoItemInlineDetail from './TodoItemInlineDetail.vue';
import TodoPriorityFlag from './TodoPriorityFlag.vue';

const props = defineProps<{
  item: TodoItem;
}>();

const store = useTodoStore();
const selection = useTodoSelection();

const isAnimating = ref(false);
let animationTimer: ReturnType<typeof setTimeout> | null = null;

const isExpanded = computed(
  () => store.selectedItemId === props.item.id && !!store.selectedItemDetail,
);
const isSelected = computed(() => selection.isSelected(props.item.id));
const isDone = computed(() => props.item.status === 'Done');

const showProjectBadge = computed(() => {
  const v = store.currentView;
  return (
    !!props.item.projectName &&
    (v === 'all' || v === 'search' || v === 'today' || v === 'upcoming')
  );
});

async function handleToggle(e: Event) {
  e.stopPropagation();
  isAnimating.value = true;
  await store.toggleComplete(props.item.id);
  animationTimer = setTimeout(() => {
    isAnimating.value = false;
  }, 280);
}

function handleRowClick(e: MouseEvent) {
  // 多選熱鍵
  if (e.metaKey || e.ctrlKey) {
    e.preventDefault();
    selection.toggle(props.item.id);
    return;
  }
  if (e.shiftKey) {
    e.preventDefault();
    selection.selectRange(props.item.id);
    return;
  }
  // 一般點擊：開合 inline detail
  if (isExpanded.value) {
    store.closeDetail();
  } else {
    store.selectItem(props.item.id);
  }
}

function handleSelectionCheck(e: Event) {
  e.stopPropagation();
  selection.toggle(props.item.id);
}

onUnmounted(() => {
  if (animationTimer !== null) clearTimeout(animationTimer);
});
</script>

<template>
  <div
    class="todo-row group cursor-pointer"
    :data-selected="isExpanded ? 'true' : 'false'"
    :class="{ 'is-multi-selected': isSelected }"
    @click="handleRowClick"
  >
    <div class="flex items-center gap-2 px-3 py-2">
      <!-- 多選 checkbox（hover or 已有選取時可見）-->
      <button
        class="flex size-4 flex-shrink-0 cursor-pointer items-center justify-center rounded border transition-all"
        :class="[
          isSelected
            ? 'border-primary bg-primary text-primary-foreground opacity-100'
            : 'border-border opacity-0 hover:border-primary group-hover:opacity-100',
          selection.hasSelection.value ? '!opacity-100' : '',
        ]"
        title="多選"
        type="button"
        @click="handleSelectionCheck"
      >
        <svg
          v-if="isSelected"
          class="size-2.5"
          fill="none"
          stroke="currentColor"
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="3"
          viewBox="0 0 24 24"
        >
          <path :d="todoIcons.check" />
        </svg>
      </button>

      <!-- 完成 checkbox -->
      <button
        class="relative flex size-5 flex-shrink-0 cursor-pointer items-center justify-center rounded-full border-2 transition-all"
        :class="[
          isDone
            ? 'border-green-500 bg-green-500'
            : 'border-border hover:border-primary',
          isAnimating ? 'scale-90' : 'scale-100',
        ]"
        type="button"
        @click="handleToggle"
      >
        <svg
          v-if="isDone"
          class="size-3 text-white"
          fill="none"
          stroke="currentColor"
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="3"
          viewBox="0 0 24 24"
        >
          <path :d="todoIcons.check" />
        </svg>
      </button>

      <!-- 標題 + 副資訊 -->
      <div class="flex min-w-0 flex-1 flex-col">
        <span
          class="truncate text-sm transition-all"
          :class="
            isDone
              ? 'text-muted-foreground line-through'
              : 'text-foreground'
          "
        >
          {{ item.title }}
        </span>
        <div
          v-if="showProjectBadge || item.tags.length > 0"
          class="mt-0.5 flex flex-wrap items-center gap-1.5"
        >
          <span
            v-if="showProjectBadge"
            class="inline-flex items-center gap-1 text-xs text-muted-foreground"
          >
            <span
              class="size-1.5 rounded-full"
              :style="{ backgroundColor: item.projectColor ?? '#94A3B8' }"
            />
            {{ item.projectName }}
          </span>
          <span
            v-for="tag in item.tags.slice(0, 3)"
            :key="tag.id"
            class="inline-flex items-center gap-1 rounded-full border px-1.5 py-px text-[10px]"
            :style="{
              borderColor: tag.color ?? '#94A3B8',
              color: tag.color ?? '#64748B',
            }"
          >
            #{{ tag.name }}
          </span>
          <span
            v-if="item.tags.length > 3"
            class="text-[10px] text-muted-foreground"
          >
            +{{ item.tags.length - 3 }}
          </span>
        </div>
      </div>

      <!-- Right badges -->
      <div class="flex flex-shrink-0 items-center gap-1.5">
        <span
          v-if="item.difficulty !== 'None'"
          class="inline-flex size-5 items-center justify-center rounded text-[10px] font-bold text-white"
          :style="{ backgroundColor: getDifficultyColor(item.difficulty) }"
          :title="`難度：${getDifficultyShortLabel(item.difficulty)}`"
        >
          {{ getDifficultyShortLabel(item.difficulty) }}
        </span>
        <TodoDateBadge :due-date="item.dueDate" />
        <TodoPriorityFlag :priority="item.priority" />
      </div>
    </div>

    <!-- Inline Detail（同時最多一筆展開）-->
    <Transition name="todo-expand">
      <TodoItemInlineDetail
        v-if="isExpanded && store.selectedItemDetail"
        :item="store.selectedItemDetail"
      />
    </Transition>
  </div>
</template>

<style scoped>
.todo-row {
  border-radius: var(--todo-radius);
  transition: background-color 180ms ease;
}
.todo-row:hover {
  background: rgb(var(--todo-row-hover));
}
.todo-row[data-selected='true'] {
  background: rgb(var(--todo-row-selected));
}
.todo-row.is-multi-selected {
  background: rgb(var(--todo-row-selected));
  box-shadow: inset 2px 0 0 0 hsl(var(--primary));
}

.todo-expand-enter-active,
.todo-expand-leave-active {
  transition: opacity 0.2s ease, max-height 0.25s ease;
  overflow: hidden;
  max-height: 800px;
}
.todo-expand-enter-from,
.todo-expand-leave-to {
  opacity: 0;
  max-height: 0;
}
</style>
