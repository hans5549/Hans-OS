<script setup lang="ts">
import type { ChecklistItem, TodoDifficulty, TodoItemDetail, TodoPriority, TodoStatus, TodoTag } from '#/api/core/todos';

import { computed, ref, watch } from 'vue';

import { Modal, message } from 'ant-design-vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

const props = defineProps<{
  item: TodoItemDetail;
}>();

const emit = defineEmits<{
  close: [];
}>();

const store = useTodoStore();

// Local editable state
const title = ref(props.item.title);
const description = ref(props.item.description ?? '');
const priority = ref<TodoPriority>(props.item.priority);
const difficulty = ref<TodoDifficulty>(props.item.difficulty);
const status = ref<TodoStatus>(props.item.status);
const dueDate = ref(props.item.dueDate ?? '');
const scheduledDate = ref(props.item.scheduledDate ?? '');
const projectId = ref<null | string>(props.item.projectId);
const isDirty = ref(false);

// Checklist
const newChecklistTitle = ref('');

// Tags
const showTagPicker = ref(false);

watch(
  () => props.item,
  (newItem) => {
    title.value = newItem.title;
    description.value = newItem.description ?? '';
    priority.value = newItem.priority;
    difficulty.value = newItem.difficulty;
    status.value = newItem.status;
    dueDate.value = newItem.dueDate ?? '';
    scheduledDate.value = newItem.scheduledDate ?? '';
    projectId.value = newItem.projectId;
    isDirty.value = false;
  },
);

function markDirty() {
  isDirty.value = true;
}

async function save() {
  if (!title.value.trim()) return;
  await store.updateItem(props.item.id, {
    description: description.value || null,
    difficulty: difficulty.value,
    dueDate: dueDate.value || null,
    priority: priority.value,
    projectId: projectId.value,
    scheduledDate: scheduledDate.value || null,
    status: status.value,
    tagIds: props.item.tags.map((t: TodoTag) => t.id),
    title: title.value.trim(),
  });
  isDirty.value = false;
}

function confirmDelete() {
  Modal.confirm({
    cancelText: $t('page.todo.cancel'),
    okText: $t('page.todo.delete'),
    okType: 'danger',
    onOk: async () => {
      await store.deleteItem(props.item.id);
      emit('close');
    },
    title: $t('page.todo.confirmDelete'),
  });
}

async function confirmPermanentDelete() {
  Modal.confirm({
    cancelText: $t('page.todo.cancel'),
    content: '永久刪除後無法復原',
    okText: '永久刪除',
    okType: 'danger',
    onOk: async () => {
      await store.permanentDeleteItem(props.item.id);
      emit('close');
    },
    title: '確認永久刪除？',
  });
}

async function handleArchive() {
  const isArchived = props.item.archivedAt !== null;
  await store.archiveItem(props.item.id, !isArchived);
  message.success(isArchived ? '已取消封存' : '已封存');
}

async function handleRestore() {
  await store.restoreItem(props.item.id);
  emit('close');
  message.success('已從垃圾桶還原');
}

// Checklist
async function addChecklist() {
  const t = newChecklistTitle.value.trim();
  if (!t) return;
  await store.addChecklistItem(props.item.id, { title: t });
  newChecklistTitle.value = '';
}

const checklistProgress = computed(() => {
  const total = props.item.checklistItems.length;
  const done = props.item.checklistItems.filter((c: ChecklistItem) => c.isCompleted).length;
  return { done, total };
});

// Tags
const availableTags = computed(() =>
  store.tags.filter((t: TodoTag) => !props.item.tags.some((it: TodoTag) => it.id === t.id)),
);

async function addTag(tagId: string) {
  const newTagIds = [...props.item.tags.map((t: TodoTag) => t.id), tagId];
  await store.updateItem(props.item.id, {
    description: props.item.description,
    difficulty: props.item.difficulty,
    dueDate: props.item.dueDate,
    priority: props.item.priority,
    projectId: props.item.projectId,
    scheduledDate: props.item.scheduledDate,
    status: props.item.status,
    tagIds: newTagIds,
    title: props.item.title,
  });
  showTagPicker.value = false;
}

async function removeTag(tagId: string) {
  const newTagIds = props.item.tags.filter((t: TodoTag) => t.id !== tagId).map((t: TodoTag) => t.id);
  await store.updateItem(props.item.id, {
    description: props.item.description,
    difficulty: props.item.difficulty,
    dueDate: props.item.dueDate,
    priority: props.item.priority,
    projectId: props.item.projectId,
    scheduledDate: props.item.scheduledDate,
    status: props.item.status,
    tagIds: newTagIds,
    title: props.item.title,
  });
}

const isInTrash = computed(() => store.currentView === 'trash');

const priorityOptions: { color: string; label: string; value: TodoPriority }[] = [
  { color: '#7C3AED', label: 'P0 緊急', value: 'Urgent' },
  { color: '#EF4444', label: 'P1 高', value: 'High' },
  { color: '#F97316', label: 'P2 中', value: 'Medium' },
  { color: '#3B82F6', label: 'P3 低', value: 'Low' },
  { color: '#94A3B8', label: 'P4 無', value: 'None' },
];

const difficultyOptions: { color: string; label: string; value: TodoDifficulty }[] = [
  { color: '#EF4444', label: '困難', value: 'Hard' },
  { color: '#F97316', label: '中等', value: 'Medium' },
  { color: '#22C55E', label: '容易', value: 'Easy' },
  { color: '#94A3B8', label: '無', value: 'None' },
];

const statusOptions: { color: string; label: string; value: TodoStatus }[] = [
  { color: '#94A3B8', label: '待處理', value: 'Pending' },
  { color: '#3B82F6', label: '進行中', value: 'InProgress' },
  { color: '#22C55E', label: '已完成', value: 'Done' },
];
</script>

<template>
  <div
    class="flex h-full flex-col border-l border-border bg-background/80 backdrop-blur-xl"
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
        rows="3"
        @input="markDirty"
      />

      <!-- Status -->
      <div>
        <label class="mb-2 block text-xs font-medium text-muted-foreground">狀態</label>
        <div class="flex gap-2">
          <button
            v-for="opt in statusOptions"
            :key="opt.value"
            class="flex-1 rounded-lg py-1.5 text-xs font-medium text-white transition-all"
            :class="status === opt.value ? 'shadow-md scale-105' : 'opacity-40 hover:opacity-70'"
            :style="{ backgroundColor: opt.color }"
            type="button"
            @click="status = opt.value; markDirty()"
          >
            {{ opt.label }}
          </button>
        </div>
      </div>

      <!-- Priority -->
      <div>
        <label class="mb-2 block text-xs font-medium text-muted-foreground">
          {{ $t('page.todo.priority') }}
        </label>
        <div class="flex gap-1 flex-wrap">
          <button
            v-for="opt in priorityOptions"
            :key="opt.value"
            class="flex-1 min-w-0 rounded-lg py-1.5 text-xs font-medium text-white transition-all"
            :class="priority === opt.value ? 'shadow-md scale-105' : 'opacity-40 hover:opacity-70'"
            :style="{ backgroundColor: opt.color }"
            type="button"
            @click="priority = opt.value; markDirty()"
          >
            {{ opt.label }}
          </button>
        </div>
      </div>

      <!-- Difficulty -->
      <div>
        <label class="mb-2 block text-xs font-medium text-muted-foreground">難度</label>
        <div class="flex gap-2">
          <button
            v-for="opt in difficultyOptions"
            :key="opt.value"
            class="flex-1 rounded-lg py-1.5 text-xs font-medium text-white transition-all"
            :class="difficulty === opt.value ? 'shadow-md scale-105' : 'opacity-40 hover:opacity-70'"
            :style="{ backgroundColor: opt.color }"
            type="button"
            @click="difficulty = opt.value; markDirty()"
          >
            {{ opt.label }}
          </button>
        </div>
      </div>

      <!-- Dates -->
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="mb-1 block text-xs font-medium text-muted-foreground">
            {{ $t('page.todo.dueDate') }}
          </label>
          <input
            v-model="dueDate"
            class="w-full rounded-lg border border-border bg-transparent px-3 py-1.5 text-sm text-foreground outline-none focus:border-primary"
            type="date"
            @change="markDirty"
          />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-muted-foreground">計劃日期</label>
          <input
            v-model="scheduledDate"
            class="w-full rounded-lg border border-border bg-transparent px-3 py-1.5 text-sm text-foreground outline-none focus:border-primary"
            type="date"
            @change="markDirty"
          />
        </div>
      </div>

      <!-- Project -->
      <div>
        <label class="mb-1 block text-xs font-medium text-muted-foreground">專案</label>
        <select
          v-model="projectId"
          class="w-full rounded-lg border border-border bg-transparent px-3 py-1.5 text-sm text-foreground outline-none focus:border-primary"
          @change="markDirty"
        >
          <option :value="null">Inbox（無專案）</option>
          <option
            v-for="p in store.projects.filter((p) => !p.isArchived)"
            :key="p.id"
            :value="p.id"
          >
            {{ p.name }}
          </option>
        </select>
      </div>

      <!-- Tags -->
      <div>
        <div class="mb-2 flex items-center justify-between">
          <label class="text-xs font-medium text-muted-foreground">標籤</label>
          <button
            class="text-xs text-primary hover:underline"
            type="button"
            @click="showTagPicker = !showTagPicker"
          >
            + 新增
          </button>
        </div>
        <div class="flex flex-wrap gap-1.5">
          <span
            v-for="tag in item.tags"
            :key="tag.id"
            class="flex items-center gap-1 rounded-full px-2 py-0.5 text-xs text-white"
            :style="{ backgroundColor: tag.color ?? '#64748B' }"
          >
            {{ tag.name }}
            <button
              class="opacity-70 hover:opacity-100"
              type="button"
              @click="removeTag(tag.id)"
            >
              ×
            </button>
          </span>
          <span v-if="item.tags.length === 0" class="text-xs text-muted-foreground">無標籤</span>
        </div>
        <!-- Tag picker -->
        <div v-if="showTagPicker && availableTags.length > 0" class="mt-2 rounded-lg border border-border p-2 space-y-1">
          <button
            v-for="tag in availableTags"
            :key="tag.id"
            class="flex w-full items-center gap-2 rounded px-2 py-1 text-xs hover:bg-accent"
            type="button"
            @click="addTag(tag.id)"
          >
            <span class="size-2.5 rounded-full" :style="{ backgroundColor: tag.color ?? '#64748B' }" />
            {{ tag.name }}
          </button>
        </div>
        <div v-else-if="showTagPicker" class="mt-2 text-xs text-muted-foreground">無可新增的標籤</div>
      </div>

      <!-- Checklist -->
      <div v-if="!isInTrash">
        <div class="mb-2 flex items-center justify-between">
          <label class="text-xs font-medium text-muted-foreground">
            檢查清單
            <span v-if="checklistProgress.total > 0" class="ml-1 text-muted-foreground">
              ({{ checklistProgress.done }}/{{ checklistProgress.total }})
            </span>
          </label>
        </div>
        <!-- Checklist items -->
        <div class="space-y-1">
          <div
            v-for="ci in item.checklistItems"
            :key="ci.id"
            class="flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-accent group"
          >
            <button
              class="flex size-4 flex-shrink-0 items-center justify-center rounded border transition-colors"
              :class="ci.isCompleted ? 'border-green-500 bg-green-500' : 'border-border hover:border-primary'"
              type="button"
              @click="store.toggleChecklistItem(item.id, ci)"
            >
              <svg v-if="ci.isCompleted" class="size-3 text-white" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24">
                <path d="M5 13l4 4L19 7" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
            </button>
            <span class="flex-1 text-xs text-foreground" :class="ci.isCompleted ? 'line-through text-muted-foreground' : ''">
              {{ ci.title }}
            </span>
            <button
              class="hidden group-hover:block text-xs text-red-400 hover:text-red-500"
              type="button"
              @click="store.deleteChecklistItem(item.id, ci.id)"
            >
              ×
            </button>
          </div>
        </div>
        <!-- Add checklist item -->
        <div class="mt-2 flex gap-2">
          <input
            v-model="newChecklistTitle"
            class="flex-1 rounded border border-border bg-transparent px-2 py-1 text-xs text-foreground outline-none focus:border-primary placeholder:text-muted-foreground"
            placeholder="新增子項目..."
            @keydown.enter.prevent="addChecklist"
            @keydown.esc.stop
          />
          <button
            class="rounded bg-accent px-2 py-1 text-xs hover:bg-accent/80 disabled:opacity-40"
            :disabled="!newChecklistTitle.trim()"
            type="button"
            @click="addChecklist"
          >
            新增
          </button>
        </div>
      </div>

      <!-- Sub-tasks -->
      <div v-if="!isInTrash && item.children.length > 0">
        <label class="mb-2 block text-xs font-medium text-muted-foreground">
          子任務 ({{ item.children.length }})
        </label>
        <div class="space-y-1">
          <div
            v-for="child in item.children"
            :key="child.id"
            class="flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-accent cursor-pointer"
            @click="store.selectItem(child.id)"
          >
            <div
              class="size-3 rounded-full border-2 flex-shrink-0"
              :class="child.status === 'Done' ? 'border-green-500 bg-green-500' : 'border-border'"
            />
            <span class="text-xs text-foreground" :class="child.status === 'Done' ? 'line-through text-muted-foreground' : ''">
              {{ child.title }}
            </span>
          </div>
        </div>
      </div>

      <!-- Metadata -->
      <div class="border-t border-border pt-3 text-xs text-muted-foreground space-y-1">
        <div>建立於 {{ new Date(item.createdAt).toLocaleDateString('zh-TW') }}</div>
        <div v-if="item.completedAt">完成於 {{ new Date(item.completedAt).toLocaleDateString('zh-TW') }}</div>
        <div v-if="item.archivedAt" class="text-amber-500">已封存</div>
      </div>
    </div>

    <!-- Footer -->
    <div class="flex items-center justify-between border-t border-border px-4 py-3 flex-wrap gap-2">
      <!-- Trash view actions -->
      <template v-if="isInTrash">
        <button
          class="text-xs font-medium text-blue-500 hover:text-blue-400"
          type="button"
          @click="handleRestore"
        >
          還原
        </button>
        <button
          class="text-xs font-medium text-red-500 hover:text-red-400"
          type="button"
          @click="confirmPermanentDelete"
        >
          永久刪除
        </button>
      </template>

      <!-- Normal view actions -->
      <template v-else>
        <button
          class="text-xs font-medium text-red-500 hover:text-red-400"
          type="button"
          @click="confirmDelete"
        >
          {{ $t('page.todo.delete') }}
        </button>
        <div class="flex gap-2">
          <button
            class="text-xs font-medium text-muted-foreground hover:text-foreground"
            type="button"
            @click="handleArchive"
          >
            {{ item.archivedAt ? '取消封存' : '封存' }}
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
      </template>
    </div>
  </div>
</template>
