<script setup lang="ts">
import type { TodoDifficulty, TodoPriority, TodoProject } from '#/api/core/todos';

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
const description = ref('');
const priority = ref<TodoPriority>('None');
const difficulty = ref<TodoDifficulty>('None');
const dueDate = ref<null | string>(null);
const scheduledDate = ref<null | string>(null);
const projectId = ref<null | string>(props.defaultProjectId ?? null);
const isExpanded = ref(false);

const priorityOptions: { color: string; label: string; value: TodoPriority }[] = [
  { color: '#7C3AED', label: 'P0', value: 'Urgent' },
  { color: '#EF4444', label: 'P1', value: 'High' },
  { color: '#F97316', label: 'P2', value: 'Medium' },
  { color: '#3B82F6', label: 'P3', value: 'Low' },
  { color: '#94A3B8', label: 'P4', value: 'None' },
];

const difficultyOptions: { color: string; label: string; value: TodoDifficulty }[] = [
  { color: '#EF4444', label: '難', value: 'Hard' },
  { color: '#F97316', label: '中', value: 'Medium' },
  { color: '#22C55E', label: '易', value: 'Easy' },
  { color: '#94A3B8', label: '無', value: 'None' },
];

async function submit() {
  if (!title.value.trim()) return;

  await store.createItem({
    categoryId: undefined,
    description: description.value || null,
    difficulty: difficulty.value,
    dueDate: dueDate.value || undefined,
    parentId: undefined,
    priority: priority.value,
    projectId: projectId.value,
    scheduledDate: scheduledDate.value || undefined,
    tagIds: [],
    title: title.value.trim(),
  });

  title.value = '';
  description.value = '';
  priority.value = 'None';
  difficulty.value = 'None';
  dueDate.value = null;
  scheduledDate.value = null;
  projectId.value = props.defaultProjectId ?? null;
  isExpanded.value = false;
  emit('close');
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault();
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
      @focus="isExpanded = true"
    />

    <!-- Expanded fields -->
    <div v-if="isExpanded" class="mt-2 space-y-2">
      <textarea
        v-model="description"
        class="w-full resize-none bg-transparent text-xs text-foreground placeholder:text-muted-foreground outline-none border border-border rounded px-2 py-1.5 focus:border-primary"
        :placeholder="$t('page.todo.description')"
        rows="2"
        @keydown.enter.stop
      />
    </div>

    <div class="mt-2 flex flex-wrap items-center gap-2">
      <!-- Priority selector -->
      <div class="flex items-center gap-1">
        <button
          v-for="opt in priorityOptions"
          :key="opt.value"
          class="rounded px-1.5 py-0.5 text-xs font-medium text-white transition-opacity hover:opacity-80"
          :class="priority === opt.value ? 'opacity-100 ring-2 ring-offset-1' : 'opacity-40'"
          :style="{ backgroundColor: opt.color, outlineColor: opt.color }"
          type="button"
          @click="priority = opt.value"
        >
          {{ opt.label }}
        </button>
      </div>

      <!-- Difficulty selector (show when expanded) -->
      <div v-if="isExpanded" class="flex items-center gap-1">
        <span class="text-xs text-muted-foreground">難度：</span>
        <button
          v-for="opt in difficultyOptions"
          :key="opt.value"
          class="rounded px-1.5 py-0.5 text-xs font-medium text-white transition-opacity hover:opacity-80"
          :class="difficulty === opt.value ? 'opacity-100 ring-2 ring-offset-1' : 'opacity-40'"
          :style="{ backgroundColor: opt.color, outlineColor: opt.color }"
          type="button"
          @click="difficulty = opt.value"
        >
          {{ opt.label }}
        </button>
      </div>

      <!-- Due date -->
      <div class="flex items-center gap-1">
        <span class="text-xs text-muted-foreground">截止：</span>
        <input
          v-model="dueDate"
          class="rounded border border-border px-2 py-0.5 text-xs text-muted-foreground outline-none focus:border-primary bg-transparent"
          type="date"
        />
      </div>

      <!-- Scheduled date (when expanded) -->
      <div v-if="isExpanded" class="flex items-center gap-1">
        <span class="text-xs text-muted-foreground">計劃：</span>
        <input
          v-model="scheduledDate"
          class="rounded border border-border px-2 py-0.5 text-xs text-muted-foreground outline-none focus:border-primary bg-transparent"
          type="date"
        />
      </div>

      <!-- Project selector (when expanded and no fixed project) -->
      <div v-if="isExpanded && !defaultProjectId" class="flex items-center gap-1">
        <span class="text-xs text-muted-foreground">專案：</span>
        <select
          v-model="projectId"
          class="rounded border border-border bg-transparent px-2 py-0.5 text-xs text-foreground outline-none focus:border-primary"
        >
          <option :value="null">Inbox</option>
          <option
            v-for="p in store.projects.filter((p: TodoProject) => !p.isArchived)"
            :key="p.id"
            :value="p.id"
          >
            {{ p.name }}
          </option>
        </select>
      </div>

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
