<script setup lang="ts">
import type { TodoPriority } from '#/api/core/todos';

import { ref } from 'vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

const props = defineProps<{
  defaultProjectId?: null | string;
}>();

const emit = defineEmits<{
  close: [];
}>();

const store = useTodoStore();

const title = ref('');
const priority = ref<TodoPriority>('None');
const dueDate = ref<null | string>(null);

const priorityOptions: { color: string; label: string; value: TodoPriority }[] = [
  { value: 'High', label: 'P1', color: '#EF4444' },
  { value: 'Medium', label: 'P2', color: '#F97316' },
  { value: 'Low', label: 'P3', color: '#3B82F6' },
  { value: 'None', label: 'P4', color: '#94A3B8' },
];

async function submit() {
  if (!title.value.trim()) return;

  await store.createItem({
    title: title.value.trim(),
    priority: priority.value,
    dueDate: dueDate.value || undefined,
    projectId: props.defaultProjectId,
  });

  title.value = '';
  priority.value = 'None';
  dueDate.value = null;
  emit('close');
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    submit();
  } else if (e.key === 'Escape') {
    emit('close');
  }
}
</script>

<template>
  <div
    class="glass-card p-3 transition-all"
    @keydown.esc.stop="emit('close')"
  >
    <input
      v-model="title"
      autofocus
      class="w-full bg-transparent text-sm text-foreground placeholder:text-muted-foreground outline-none"
      :placeholder="$t('page.todo.addTask')"
      @keydown="handleKeydown"
    />

    <div class="mt-2 flex items-center gap-2">
      <!-- Priority selector -->
      <div class="flex items-center gap-1">
        <button
          v-for="opt in priorityOptions"
          :key="opt.value"
          class="rounded px-1.5 py-0.5 text-xs font-medium text-white transition-opacity hover:opacity-80"
          :class="priority === opt.value ? 'opacity-100 ring-2 ring-offset-1' : 'opacity-50'"
          :style="{ backgroundColor: opt.color, outlineColor: opt.color }"
          type="button"
          @click="priority = opt.value"
        >
          {{ opt.label }}
        </button>
      </div>

      <!-- Due date -->
      <input
        v-model="dueDate"
        class="rounded border border-border px-2 py-0.5 text-xs text-muted-foreground outline-none focus:border-primary bg-transparent"
        type="date"
      />

      <div class="ml-auto flex gap-2">
        <button
          class="text-xs text-muted-foreground hover:text-foreground"
          type="button"
          @click="emit('close')"
        >
          {{ $t('page.todo.cancel') }}
        </button>
        <button
          class="rounded bg-primary px-3 py-1 text-xs font-medium text-primary-foreground hover:bg-primary-hover disabled:opacity-40"
          :disabled="!title.trim() || store.loading.saving"
          type="button"
          @click="submit"
        >
          {{ $t('page.todo.save') }}
        </button>
      </div>
    </div>
  </div>
</template>
