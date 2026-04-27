<script setup lang="ts">
import type { TodoDifficulty, TodoPriority, TodoProject } from '#/api/core/todos';

import { computed, nextTick, ref } from 'vue';

import { DatePicker, Popover } from 'ant-design-vue';
import dayjs, { type Dayjs } from 'dayjs';

import { useTodoStore } from '#/store/todo';

import {
  difficultyOptions,
  getDifficultyColor,
  getPriorityColor,
  priorityOptions,
  todoIcons,
} from '../composables/useTodoMeta';

const store = useTodoStore();

const expanded = ref(false);
const title = ref('');
const description = ref('');
const priority = ref<TodoPriority>('None');
const difficulty = ref<TodoDifficulty>('None');
const dueDate = ref<Dayjs | undefined>(undefined);
const scheduledDate = ref<Dayjs | undefined>(undefined);
const projectId = ref<null | string>(null);

const titleInputRef = ref<HTMLInputElement | null>(null);

const projectOptions = computed(() => [
  { value: null as null | string, label: 'Inbox', color: '#94A3B8' },
  ...store.projects
    .filter((p: TodoProject) => !p.isArchived)
    .map((p: TodoProject) => ({ value: p.id, label: p.name, color: p.color })),
]);

const currentProject = computed(
  () =>
    projectOptions.value.find((o) => o.value === projectId.value) ??
    projectOptions.value[0]!,
);

const currentPriorityMeta = computed(
  () => priorityOptions.find((o) => o.value === priority.value)!,
);
const currentDifficultyMeta = computed(
  () => difficultyOptions.find((o) => o.value === difficulty.value)!,
);

function reset() {
  title.value = '';
  description.value = '';
  priority.value = 'None';
  difficulty.value = 'None';
  dueDate.value = undefined;
  scheduledDate.value = undefined;
  projectId.value = null;
}

async function handleExpand() {
  expanded.value = true;
  await nextTick();
  titleInputRef.value?.focus();
}

function handleCollapse() {
  expanded.value = false;
  reset();
}

async function handleSubmit() {
  const t = title.value.trim();
  if (!t) return;
  await store.createItem({
    description: description.value.trim() || null,
    difficulty: difficulty.value,
    dueDate: dueDate.value ? dueDate.value.format('YYYY-MM-DD') : null,
    priority: priority.value,
    projectId: projectId.value,
    scheduledDate: scheduledDate.value ? scheduledDate.value.format('YYYY-MM-DD') : null,
    tagIds: [],
    title: t,
  });
  // 連續輸入：清空但保留展開狀態
  reset();
  await nextTick();
  titleInputRef.value?.focus();
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
    handleSubmit();
  } else if (e.key === 'Escape') {
    e.stopPropagation();
    handleCollapse();
  }
}

defineExpose({ expand: handleExpand });
</script>

<template>
  <div
    class="rounded-xl border border-border/60 bg-card/40 px-3 py-2 transition-all"
    :class="expanded ? 'border-primary/40 shadow-sm' : 'hover:border-border'"
  >
    <!-- Collapsed -->
    <button
      v-if="!expanded"
      class="flex w-full cursor-pointer items-center gap-2 text-left text-sm text-muted-foreground transition-colors hover:text-foreground"
      type="button"
      @click="handleExpand"
    >
      <svg
        class="size-4"
        fill="none"
        stroke="currentColor"
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        viewBox="0 0 24 24"
      >
        <path :d="todoIcons.plus" />
      </svg>
      <span>新增任務… <span class="ml-1 text-xs opacity-60">(N)</span></span>
    </button>

    <!-- Expanded -->
    <div v-else>
      <input
        ref="titleInputRef"
        v-model="title"
        class="w-full bg-transparent text-sm font-medium text-foreground placeholder:text-muted-foreground focus:outline-none"
        placeholder="輸入任務標題…"
        @keydown="handleKeydown"
      />
      <textarea
        v-model="description"
        class="mt-1 max-h-32 w-full resize-none bg-transparent text-xs text-foreground placeholder:text-muted-foreground focus:outline-none"
        placeholder="描述（可選）…"
        rows="1"
        @keydown.esc.stop="handleCollapse"
      />

      <div class="mt-2 flex flex-wrap items-center gap-1.5">
        <!-- Priority -->
        <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
          <button
            class="todo-chip cursor-pointer"
            :style="{ color: getPriorityColor(priority) }"
            type="button"
          >
            <svg
              class="size-3"
              fill="currentColor"
              viewBox="0 0 24 24"
            >
              <path :d="todoIcons.flag" />
            </svg>
            {{ currentPriorityMeta.label }}
          </button>
          <template #content>
            <div class="flex flex-col gap-1">
              <button
                v-for="opt in priorityOptions"
                :key="opt.value"
                class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
                type="button"
                @click="priority = opt.value"
              >
                <svg
                  class="size-3.5"
                  fill="currentColor"
                  :style="{ color: getPriorityColor(opt.value) }"
                  viewBox="0 0 24 24"
                >
                  <path :d="todoIcons.flag" />
                </svg>
                {{ opt.label }}
              </button>
            </div>
          </template>
        </Popover>

        <!-- Difficulty -->
        <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
          <button
            class="todo-chip cursor-pointer"
            :style="{ color: getDifficultyColor(difficulty) }"
            type="button"
          >
            難度：{{ currentDifficultyMeta.label }}
          </button>
          <template #content>
            <div class="flex flex-col gap-1">
              <button
                v-for="opt in difficultyOptions"
                :key="opt.value"
                class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
                type="button"
                @click="difficulty = opt.value"
              >
                <span
                  class="size-2.5 rounded-full"
                  :style="{ backgroundColor: getDifficultyColor(opt.value) }"
                />
                {{ opt.label }}
              </button>
            </div>
          </template>
        </Popover>

        <!-- Due -->
        <DatePicker
          v-model:value="dueDate"
          format="YYYY-MM-DD"
          placeholder="截止日"
          size="small"
        >
          <template #renderExtraFooter>
            <div class="flex gap-2 py-1">
              <button
                class="cursor-pointer rounded px-2 py-1 text-xs text-primary hover:bg-accent"
                type="button"
                @click="dueDate = dayjs()"
              >
                今天
              </button>
              <button
                class="cursor-pointer rounded px-2 py-1 text-xs text-primary hover:bg-accent"
                type="button"
                @click="dueDate = dayjs().add(1, 'day')"
              >
                明天
              </button>
              <button
                class="cursor-pointer rounded px-2 py-1 text-xs text-primary hover:bg-accent"
                type="button"
                @click="dueDate = dayjs().add(7, 'day')"
              >
                下週
              </button>
            </div>
          </template>
        </DatePicker>

        <!-- Scheduled -->
        <DatePicker
          v-model:value="scheduledDate"
          format="YYYY-MM-DD"
          placeholder="計畫日"
          size="small"
        />

        <!-- Project -->
        <Popover :overlay-inner-style="{ padding: '6px', width: '220px' }" trigger="click">
          <button class="todo-chip cursor-pointer" type="button">
            <span
              class="size-2.5 rounded-full"
              :style="{ backgroundColor: currentProject.color }"
            />
            {{ currentProject.label }}
          </button>
          <template #content>
            <div class="flex max-h-64 flex-col gap-1 overflow-y-auto">
              <button
                v-for="opt in projectOptions"
                :key="opt.value ?? 'inbox'"
                class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
                type="button"
                @click="projectId = opt.value"
              >
                <span
                  class="size-2.5 rounded-full"
                  :style="{ backgroundColor: opt.color }"
                />
                <span class="flex-1 truncate">{{ opt.label }}</span>
              </button>
            </div>
          </template>
        </Popover>

        <div class="ml-auto flex items-center gap-1.5">
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            type="button"
            @click="handleCollapse"
          >
            取消
          </button>
          <button
            class="cursor-pointer rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
            :disabled="!title.trim() || store.loading.saving"
            type="button"
            @click="handleSubmit"
          >
            新增 ⏎
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
