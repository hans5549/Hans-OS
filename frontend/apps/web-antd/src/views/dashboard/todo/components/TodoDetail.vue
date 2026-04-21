<script setup lang="ts">
import type { TodoItem, TodoPriority, UpdateItemRequest } from '#/api/core/todos';

import { ref, watch } from 'vue';

import { Modal } from 'ant-design-vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

const props = defineProps<{
  item: TodoItem;
}>();

const emit = defineEmits<{
  close: [];
}>();

const store = useTodoStore();

const title = ref(props.item.title);
const description = ref(props.item.description ?? '');
const priority = ref<TodoPriority>(props.item.priority);
const dueDate = ref(props.item.dueDate ?? '');
const isDirty = ref(false);

watch(
  () => props.item,
  (newItem) => {
    title.value = newItem.title;
    description.value = newItem.description ?? '';
    priority.value = newItem.priority;
    dueDate.value = newItem.dueDate ?? '';
    isDirty.value = false;
  },
);

function markDirty() {
  isDirty.value = true;
}

async function save() {
  if (!title.value.trim()) return;

  const data: UpdateItemRequest = {
    title: title.value.trim(),
    description: description.value || null,
    priority: priority.value,
    status: props.item.status,
    dueDate: dueDate.value || null,
    projectId: props.item.projectId,
  };

  await store.updateItem(props.item.id, data);
  isDirty.value = false;
}

function confirmDelete() {
  Modal.confirm({
    title: $t('page.todo.confirmDelete'),
    okType: 'danger',
    okText: $t('page.todo.delete'),
    cancelText: $t('page.todo.cancel'),
    onOk: async () => {
      await store.deleteItem(props.item.id);
      emit('close');
    },
  });
}

const priorityOptions: { color: string; label: string; value: TodoPriority }[] = [
  { value: 'High', label: 'P1 高', color: '#EF4444' },
  { value: 'Medium', label: 'P2 中', color: '#F97316' },
  { value: 'Low', label: 'P3 低', color: '#3B82F6' },
  { value: 'None', label: 'P4 無', color: '#94A3B8' },
];
</script>

<template>
  <div
    class="flex h-full flex-col border-l border-border bg-background"
    @keydown.esc.stop="emit('close')"
  >
    <!-- Header -->
    <div class="flex items-center justify-between border-b border-border px-4 py-3">
      <span class="text-sm font-medium text-muted-foreground">任務詳情</span>
      <button
        class="rounded p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
        type="button"
        @click="emit('close')"
      >
        <svg class="size-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path d="M6 18L18 6M6 6l12 12" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
        </svg>
      </button>
    </div>

    <!-- Body -->
    <div class="flex-1 overflow-y-auto p-4 space-y-4">
      <!-- Title -->
      <textarea
        v-model="title"
        class="w-full resize-none border-0 bg-transparent text-base font-medium text-foreground outline-none placeholder:text-muted-foreground"
        placeholder="任務標題"
        rows="2"
        @input="markDirty"
      />

      <!-- Description -->
      <textarea
        v-model="description"
        class="glass-input w-full resize-none rounded-lg border border-border p-2.5 text-sm text-foreground outline-none placeholder:text-muted-foreground focus:border-primary"
        :placeholder="$t('page.todo.description')"
        rows="4"
        @input="markDirty"
      />

      <!-- Due Date -->
      <div>
        <label class="mb-1 block text-xs font-medium text-muted-foreground">
          {{ $t('page.todo.dueDate') }}
        </label>
        <input
          v-model="dueDate"
          class="rounded-lg border border-border bg-transparent px-3 py-1.5 text-sm text-foreground outline-none focus:border-primary"
          type="date"
          @change="markDirty"
        />
      </div>

      <!-- Priority -->
      <div>
        <label class="mb-2 block text-xs font-medium text-muted-foreground">
          {{ $t('page.todo.priority') }}
        </label>
        <div class="flex gap-2">
          <button
            v-for="opt in priorityOptions"
            :key="opt.value"
            class="flex-1 rounded-lg py-1.5 text-xs font-medium text-white transition-all"
            :class="priority === opt.value ? 'shadow-md scale-105' : 'opacity-40 hover:opacity-70'"
            :style="{ backgroundColor: opt.color }"
            type="button"
            @click="priority = opt.value; markDirty()"
          >
            {{ opt.label }}
          </button>
        </div>
      </div>

      <!-- Project -->
      <div v-if="item.projectName">
        <label class="mb-1 block text-xs font-medium text-muted-foreground">專案</label>
        <div class="flex items-center gap-2">
          <span
            class="size-3 rounded-full"
            :style="{ backgroundColor: item.projectColor ?? '#94A3B8' }"
          />
          <span class="text-sm text-foreground">{{ item.projectName }}</span>
        </div>
      </div>
    </div>

    <!-- Footer -->
    <div class="flex items-center justify-between border-t border-border px-4 py-3">
      <button
        class="text-xs font-medium text-red-500 hover:text-red-400"
        type="button"
        @click="confirmDelete"
      >
        {{ $t('page.todo.delete') }}
      </button>
      <button
        class="rounded-lg bg-primary px-4 py-1.5 text-xs font-medium text-primary-foreground hover:bg-primary-hover disabled:opacity-40"
        :disabled="!isDirty || store.loading.saving"
        type="button"
        @click="save"
      >
        {{ $t('page.todo.save') }}
      </button>
    </div>
  </div>
</template>
