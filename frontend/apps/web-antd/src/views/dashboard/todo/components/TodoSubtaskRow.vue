<script setup lang="ts">
import type { TodoItem, TodoPriority, UpdateItemRequest } from '#/api/core/todos';

import { computed, ref, watch } from 'vue';

import { DatePicker, Modal, Popover } from 'ant-design-vue';
import dayjs, { type Dayjs } from 'dayjs';

import { useTodoStore } from '#/store/todo';

import {
  getPriorityColor,
  priorityOptions,
  todoIcons,
} from '../composables/useTodoMeta';
import TodoDateBadge from './TodoDateBadge.vue';
import TodoPriorityFlag from './TodoPriorityFlag.vue';

const props = defineProps<{
  isInTrash?: boolean;
  parentId: string;
  subtask: TodoItem;
}>();

const emit = defineEmits<{
  dragOver: [event: DragEvent];
  dragStart: [id: string];
  drop: [id: string];
}>();

const store = useTodoStore();
const editing = ref(false);
const titleDraft = ref(props.subtask.title);

const isDone = computed(() => props.subtask.status === 'Done');
const dueDateValue = computed(() =>
  props.subtask.dueDate ? dayjs(props.subtask.dueDate) : undefined,
);

watch(
  () => props.subtask.title,
  (value) => {
    titleDraft.value = value;
  },
);

function buildUpdateRequest(): UpdateItemRequest {
  return {
    description: props.subtask.description,
    difficulty: props.subtask.difficulty,
    dueDate: props.subtask.dueDate,
    categoryId: props.subtask.categoryId,
    parentId: props.parentId,
    priority: props.subtask.priority,
    projectId: props.subtask.projectId,
    recurrenceInterval: props.subtask.recurrenceInterval,
    recurrencePattern: props.subtask.recurrencePattern,
    scheduledDate: props.subtask.scheduledDate,
    status: props.subtask.status,
    tagIds: props.subtask.tags.map((tag) => tag.id),
    title: props.subtask.title,
  };
}

async function saveTitle() {
  const title = titleDraft.value.trim();
  editing.value = false;
  if (!title || title === props.subtask.title) return;
  await store.updateItem(props.subtask.id, { ...buildUpdateRequest(), title });
}

async function setPriority(priority: TodoPriority) {
  await store.updateItem(props.subtask.id, { ...buildUpdateRequest(), priority });
}

async function setDueDate(value: Dayjs | null | string) {
  const dueDate = value && typeof value !== 'string' ? value.format('YYYY-MM-DD') : null;
  await store.updateItem(props.subtask.id, { ...buildUpdateRequest(), dueDate });
}

function confirmDelete() {
  Modal.confirm({
    cancelText: '取消',
    okText: '刪除',
    okType: 'danger',
    onOk: () => store.deleteItem(props.subtask.id),
    title: '刪除子任務？',
  });
}
</script>

<template>
  <div
    class="group flex items-center gap-2 rounded-md px-2 py-1.5 text-sm transition-colors hover:bg-accent/70"
    draggable="true"
    @dragover.prevent="emit('dragOver', $event)"
    @dragstart="emit('dragStart', subtask.id)"
    @drop.prevent="emit('drop', subtask.id)"
  >
    <span
      class="cursor-grab text-muted-foreground opacity-40 transition-opacity group-hover:opacity-100"
      title="拖曳排序"
    >
      ⋮⋮
    </span>

    <button
      class="flex size-4 flex-shrink-0 cursor-pointer items-center justify-center rounded-full border transition-colors"
      :class="
        isDone
          ? 'border-green-500 bg-green-500'
          : 'border-border hover:border-primary'
      "
      type="button"
      @click.stop="store.toggleComplete(subtask.id)"
    >
      <svg
        v-if="isDone"
        class="size-2.5 text-white"
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

    <input
      v-if="editing"
      v-model="titleDraft"
      class="min-w-0 flex-1 rounded border border-border bg-background px-2 py-0.5 text-sm outline-none focus:border-primary"
      autofocus
      @blur="saveTitle"
      @keydown.enter.prevent="saveTitle"
      @keydown.esc.stop="editing = false"
    />
    <button
      v-else
      class="min-w-0 flex-1 cursor-text truncate text-left"
      :class="isDone ? 'text-muted-foreground line-through' : 'text-foreground'"
      type="button"
      @dblclick.stop="editing = true"
    >
      {{ subtask.title }}
    </button>

    <div class="flex items-center gap-1 opacity-70 transition-opacity group-hover:opacity-100">
      <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
        <button class="cursor-pointer" title="優先級" type="button" @click.stop>
          <TodoPriorityFlag :priority="subtask.priority" />
        </button>
        <template #content>
          <div class="flex flex-col gap-1">
            <button
              v-for="opt in priorityOptions"
              :key="opt.value"
              class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
              type="button"
              @click="setPriority(opt.value)"
            >
              <span class="size-2 rounded-full" :style="{ backgroundColor: getPriorityColor(opt.value) }" />
              {{ opt.label }}
            </button>
          </div>
        </template>
      </Popover>

      <DatePicker
        :bordered="false"
        :value="dueDateValue"
        format="YYYY-MM-DD"
        size="small"
        @change="setDueDate"
        @click.stop
      >
        <template #suffixIcon>
          <TodoDateBadge :due-date="subtask.dueDate" />
        </template>
      </DatePicker>

      <button
        v-if="!isInTrash"
        class="cursor-pointer rounded px-1 text-muted-foreground transition-colors hover:bg-red-500/10 hover:text-red-500"
        title="刪除"
        type="button"
        @click.stop="confirmDelete"
      >
        ×
      </button>
    </div>
  </div>
</template>
