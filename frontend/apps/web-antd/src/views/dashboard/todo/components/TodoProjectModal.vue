<script setup lang="ts">
import type { TodoProject } from '#/api/core/todos';

import { reactive, ref, watch } from 'vue';

import { Form, FormItem, Input, Popconfirm } from 'ant-design-vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

const props = defineProps<{
  open: boolean;
  project?: null | TodoProject;
}>();

const emit = defineEmits<{
  'update:open': [boolean];
}>();

const store = useTodoStore();

const colorOptions = [
  '#EF4444', '#F97316', '#EAB308', '#22C55E', '#14B8A6',
  '#3B82F6', '#8B5CF6', '#EC4899', '#64748B', '#1E293B',
];

const formState = reactive({
  name: '',
  color: '#3B82F6',
});

const errorText = ref('');

watch(
  () => [props.open, props.project] as const,
  ([open, project]) => {
    if (!open) return;
    formState.name = project?.name ?? '';
    formState.color = project?.color ?? '#3B82F6';
    errorText.value = '';
  },
  { immediate: true },
);

async function handleSave() {
  const trimmed = formState.name.trim();
  if (!trimmed) {
    errorText.value = '請輸入專案名稱';
    return;
  }
  if (props.project) {
    await store.updateProject(props.project.id, {
      color: formState.color,
      isArchived: props.project.isArchived,
      name: trimmed,
    });
  } else {
    await store.createProject({ color: formState.color, name: trimmed });
  }
  emit('update:open', false);
}

async function handleDelete() {
  if (!props.project) return;
  await store.deleteProject(props.project.id);
  emit('update:open', false);
}

function handleCancel() {
  emit('update:open', false);
}
</script>

<template>
  <a-modal
    :cancel-text="$t('page.todo.cancel')"
    :confirm-loading="store.loading.saving"
    :ok-text="$t('page.todo.save')"
    :open="open"
    :title="project ? $t('page.todo.editProject') : $t('page.todo.newProject')"
    :width="440"
    @cancel="handleCancel"
    @ok="handleSave"
  >
    <Form layout="vertical" class="py-2">
      <FormItem
        :label="$t('page.todo.projectName')"
        :validate-status="errorText ? 'error' : ''"
        :help="errorText || undefined"
      >
        <Input
          v-model:value="formState.name"
          autofocus
          :maxlength="100"
          :placeholder="$t('page.todo.projectName')"
          @press-enter="handleSave"
        />
      </FormItem>

      <FormItem :label="$t('page.todo.projectColor')">
        <div class="flex flex-wrap gap-2">
          <button
            v-for="c in colorOptions"
            :key="c"
            class="relative size-8 cursor-pointer rounded-full border-2 transition-all hover:opacity-80"
            :class="
              formState.color === c
                ? 'border-foreground'
                : 'border-transparent'
            "
            :style="{ backgroundColor: c }"
            type="button"
            @click="formState.color = c"
          >
            <svg
              v-if="formState.color === c"
              class="absolute inset-0 m-auto size-4 text-white"
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="3"
              viewBox="0 0 24 24"
            >
              <path d="M20 6 9 17l-5-5" />
            </svg>
          </button>
        </div>
      </FormItem>
    </Form>

    <template v-if="project" #footer>
      <div class="flex items-center justify-between">
        <Popconfirm
          cancel-text="取消"
          ok-text="刪除"
          ok-type="danger"
          :title="$t('page.todo.confirmDeleteProject')"
          @confirm="handleDelete"
        >
          <button
            class="cursor-pointer text-sm font-medium text-red-500 transition-colors hover:text-red-600"
            type="button"
          >
            {{ $t('page.todo.delete') }}
          </button>
        </Popconfirm>
        <div class="flex gap-2">
          <a-button @click="handleCancel">{{ $t('page.todo.cancel') }}</a-button>
          <a-button
            :loading="store.loading.saving"
            type="primary"
            @click="handleSave"
          >
            {{ $t('page.todo.save') }}
          </a-button>
        </div>
      </div>
    </template>
  </a-modal>
</template>
