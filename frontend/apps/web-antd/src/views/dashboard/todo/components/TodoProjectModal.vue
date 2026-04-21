<script setup lang="ts">
import type { TodoProject } from '#/api/core/todos';

import { ref, watch } from 'vue';

import { Modal } from 'ant-design-vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

const props = defineProps<{
  open: boolean;
  project?: TodoProject | null;
}>();

const emit = defineEmits<{
  'update:open': [boolean];
}>();

const store = useTodoStore();

const name = ref('');
const color = ref('#3B82F6');
const isArchived = ref(false);

const colorOptions = [
  '#EF4444', '#F97316', '#EAB308', '#22C55E',
  '#3B82F6', '#8B5CF6', '#EC4899', '#14B8A6',
  '#64748B', '#1E293B',
];

watch(
  () => props.project,
  (p) => {
    name.value = p?.name ?? '';
    color.value = p?.color ?? '#3B82F6';
    isArchived.value = p?.isArchived ?? false;
  },
  { immediate: true },
);

async function handleSave() {
  if (!name.value.trim()) return;

  if (props.project) {
    await store.updateProject(props.project.id, {
      name: name.value.trim(),
      color: color.value,
      isArchived: isArchived.value,
    });
  } else {
    await store.createProject({ name: name.value.trim(), color: color.value });
  }

  emit('update:open', false);
}

function handleDelete() {
  if (!props.project) return;

  Modal.confirm({
    title: $t('page.todo.confirmDeleteProject'),
    okType: 'danger',
    okText: $t('page.todo.delete'),
    cancelText: $t('page.todo.cancel'),
    onOk: async () => {
      await store.deleteProject(props.project!.id);
      emit('update:open', false);
    },
  });
}
</script>

<template>
  <a-modal
    :open="open"
    :title="project ? $t('page.todo.editProject') : $t('page.todo.newProject')"
    :ok-text="$t('page.todo.save')"
    :cancel-text="$t('page.todo.cancel')"
    :confirm-loading="store.loading.saving"
    @ok="handleSave"
    @cancel="emit('update:open', false)"
  >
    <div class="space-y-4 py-2">
      <!-- Name -->
      <div>
        <label class="mb-1 block text-sm font-medium text-slate-700">
          {{ $t('page.todo.projectName') }}
        </label>
        <a-input
          v-model:value="name"
          :maxlength="100"
          :placeholder="$t('page.todo.projectName')"
          autofocus
        />
      </div>

      <!-- Color Picker -->
      <div>
        <label class="mb-2 block text-sm font-medium text-slate-700">
          {{ $t('page.todo.projectColor') }}
        </label>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="c in colorOptions"
            :key="c"
            class="size-8 rounded-full border-2 transition-transform hover:scale-110"
            :class="color === c ? 'border-slate-800 scale-110' : 'border-transparent'"
            :style="{ backgroundColor: c }"
            type="button"
            @click="color = c"
          />
        </div>
      </div>

      <!-- Delete (edit mode only) -->
      <div v-if="project" class="pt-2">
        <button
          class="text-sm font-medium text-red-500 hover:text-red-700"
          type="button"
          @click="handleDelete"
        >
          {{ $t('page.todo.confirmDeleteProject') }}
        </button>
      </div>
    </div>
  </a-modal>
</template>
