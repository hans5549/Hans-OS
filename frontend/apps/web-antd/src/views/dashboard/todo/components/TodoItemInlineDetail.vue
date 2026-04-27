<script setup lang="ts">
import type {
  ChecklistItem,
  TodoDifficulty,
  TodoItemDetail,
  TodoPriority,
  TodoProject,
  TodoStatus,
  TodoTag,
} from '#/api/core/todos';

import { computed, ref, watch } from 'vue';

import {
  DatePicker,
  message,
  Modal,
  Popover,
  Select,
  SelectOption,
  Textarea,
} from 'ant-design-vue';
import dayjs, { type Dayjs } from 'dayjs';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import {
  difficultyOptions,
  getDifficultyColor,
  getPriorityColor,
  priorityOptions,
  statusOptions,
  todoIcons,
} from '../composables/useTodoMeta';

const props = defineProps<{
  item: TodoItemDetail;
}>();

const store = useTodoStore();

// ── Local editable state（描述以本地 buffer 處理；其餘欄位即時提交）──
const description = ref(props.item.description ?? '');
const newChecklistTitle = ref('');

watch(
  () => props.item.id,
  () => {
    description.value = props.item.description ?? '';
    newChecklistTitle.value = '';
  },
);

watch(
  () => props.item.description,
  (v) => {
    if ((v ?? '') !== description.value) description.value = v ?? '';
  },
);

const isInTrash = computed(() => store.currentView === 'trash');

const checklistProgress = computed(() => {
  const total = props.item.checklistItems.length;
  const done = props.item.checklistItems.filter(
    (c: ChecklistItem) => c.isCompleted,
  ).length;
  return { done, total };
});

// ── Helpers ──
function buildBaseRequest() {
  return {
    description: props.item.description,
    difficulty: props.item.difficulty,
    dueDate: props.item.dueDate,
    priority: props.item.priority,
    projectId: props.item.projectId,
    scheduledDate: props.item.scheduledDate,
    status: props.item.status,
    tagIds: props.item.tags.map((t: TodoTag) => t.id),
    title: props.item.title,
  };
}

async function patch(partial: Partial<ReturnType<typeof buildBaseRequest>>) {
  await store.updateItem(props.item.id, { ...buildBaseRequest(), ...partial });
}

// ── Field handlers ──
async function setStatus(s: TodoStatus) {
  await patch({ status: s });
}

async function setPriority(p: TodoPriority) {
  await patch({ priority: p });
}

async function setDifficulty(d: TodoDifficulty) {
  await patch({ difficulty: d });
}

async function setDueDate(d: Dayjs | null | string) {
  const v = d && typeof d !== 'string' ? d.format('YYYY-MM-DD') : null;
  await patch({ dueDate: v });
}

async function setScheduledDate(d: Dayjs | null | string) {
  const v = d && typeof d !== 'string' ? d.format('YYYY-MM-DD') : null;
  await patch({ scheduledDate: v });
}

async function setProject(id: null | string) {
  await patch({ projectId: id });
}

async function setTags(tagIds: string[]) {
  await patch({ tagIds });
}

async function saveDescription() {
  if ((props.item.description ?? '') === description.value) return;
  await patch({ description: description.value || null });
}

// ── Tags：支援即時新增 ──
const tagSelectValue = computed({
  get: () => props.item.tags.map((t: TodoTag) => t.id),
  set: (val: string[]) => {
    setTags(val);
  },
});

const tagOptions = computed(() =>
  store.tags.map((t: TodoTag) => ({
    value: t.id,
    label: t.name,
    color: t.color ?? '#94A3B8',
  })),
);

// ── Checklist ──
async function addChecklist() {
  const v = newChecklistTitle.value.trim();
  if (!v) return;
  await store.addChecklistItem(props.item.id, { title: v });
  newChecklistTitle.value = '';
}

// ── Project options ──
const projectOptions = computed(() => [
  { value: null as null | string, label: 'Inbox（無專案）', color: '#94A3B8' },
  ...store.projects
    .filter((p: TodoProject) => !p.isArchived)
    .map((p: TodoProject) => ({ value: p.id, label: p.name, color: p.color })),
]);

const currentProjectMeta = computed(() => {
  const id = props.item.projectId;
  return projectOptions.value.find((o) => o.value === id) ?? projectOptions.value[0]!;
});

// ── Actions ──
function confirmDelete() {
  Modal.confirm({
    cancelText: $t('page.todo.cancel'),
    okText: $t('page.todo.delete'),
    okType: 'danger',
    onOk: async () => {
      await store.deleteItem(props.item.id);
    },
    title: $t('page.todo.confirmDelete'),
  });
}

function confirmPermanentDelete() {
  Modal.confirm({
    cancelText: $t('page.todo.cancel'),
    content: '永久刪除後將無法復原',
    okText: '永久刪除',
    okType: 'danger',
    onOk: async () => {
      await store.permanentDeleteItem(props.item.id);
    },
    title: '確認永久刪除？',
  });
}

async function handleArchive() {
  const archived = props.item.archivedAt !== null;
  await store.archiveItem(props.item.id, !archived);
  message.success(archived ? '已取消封存' : '已封存');
}

async function handleRestore() {
  await store.restoreItem(props.item.id);
  message.success('已還原');
}

// ── Date quick presets ──
function dueQuickPresets(): {
  label: string;
  value: () => Dayjs;
}[] {
  return [
    { label: '今天', value: () => dayjs() },
    { label: '明天', value: () => dayjs().add(1, 'day') },
    { label: '下週', value: () => dayjs().add(7, 'day') },
  ];
}

const dueDateValue = computed(() =>
  props.item.dueDate ? dayjs(props.item.dueDate) : undefined,
);
const scheduledDateValue = computed(() =>
  props.item.scheduledDate ? dayjs(props.item.scheduledDate) : undefined,
);

function currentStatusMeta() {
  return statusOptions.find((s) => s.value === props.item.status)!;
}

function currentPriorityMeta() {
  return priorityOptions.find((s) => s.value === props.item.priority)!;
}

function currentDifficultyMeta() {
  return difficultyOptions.find((s) => s.value === props.item.difficulty)!;
}
</script>

<template>
  <div
    class="todo-inline-detail border-t border-border/60 px-3 py-3"
    @click.stop
    @keydown.esc.stop
  >
    <!-- Description -->
    <Textarea
      v-model:value="description"
      auto-size
      class="!mb-3 !border-border/50 !bg-transparent"
      placeholder="加入描述…"
      @blur="saveDescription"
    />

    <!-- Chip row：狀態 / 優先級 / 難度 / 截止 / 計畫 / 專案 -->
    <div class="mb-3 flex flex-wrap items-center gap-1.5">
      <!-- 狀態 -->
      <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
        <button
          class="todo-chip cursor-pointer"
          :style="{ color: currentStatusMeta().color }"
          type="button"
        >
          <span
            class="size-2 rounded-full"
            :style="{ backgroundColor: currentStatusMeta().color }"
          />
          {{ currentStatusMeta().label }}
        </button>
        <template #content>
          <div class="flex flex-col gap-1">
            <button
              v-for="opt in statusOptions"
              :key="opt.value"
              class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
              type="button"
              @click="setStatus(opt.value)"
            >
              <span
                class="size-2.5 rounded-full"
                :style="{ backgroundColor: opt.color }"
              />
              <span class="flex-1">{{ opt.label }}</span>
              <svg
                v-if="item.status === opt.value"
                class="size-3.5 text-primary"
                fill="none"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.check" />
              </svg>
            </button>
          </div>
        </template>
      </Popover>

      <!-- 優先級 -->
      <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
        <button
          class="todo-chip cursor-pointer"
          :style="{ color: getPriorityColor(item.priority) }"
          type="button"
        >
          <svg
            class="size-3"
            fill="currentColor"
            stroke="currentColor"
            stroke-width="1.6"
            viewBox="0 0 24 24"
          >
            <path :d="todoIcons.flag" />
          </svg>
          {{ currentPriorityMeta().label }}
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
              <svg
                class="size-3.5"
                fill="currentColor"
                :style="{ color: getPriorityColor(opt.value) }"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.flag" />
              </svg>
              <span class="flex-1">{{ opt.label }}</span>
              <svg
                v-if="item.priority === opt.value"
                class="size-3.5 text-primary"
                fill="none"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.check" />
              </svg>
            </button>
          </div>
        </template>
      </Popover>

      <!-- 難度 -->
      <Popover :overlay-inner-style="{ padding: '6px' }" trigger="click">
        <button
          class="todo-chip cursor-pointer"
          :style="{ color: getDifficultyColor(item.difficulty) }"
          type="button"
        >
          難度：{{ currentDifficultyMeta().label }}
        </button>
        <template #content>
          <div class="flex flex-col gap-1">
            <button
              v-for="opt in difficultyOptions"
              :key="opt.value"
              class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
              type="button"
              @click="setDifficulty(opt.value)"
            >
              <span
                class="size-2.5 rounded-full"
                :style="{ backgroundColor: getDifficultyColor(opt.value) }"
              />
              <span class="flex-1">{{ opt.label }}</span>
              <svg
                v-if="item.difficulty === opt.value"
                class="size-3.5 text-primary"
                fill="none"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.check" />
              </svg>
            </button>
          </div>
        </template>
      </Popover>

      <!-- 截止日 -->
      <DatePicker
        :value="dueDateValue"
        format="YYYY-MM-DD"
        :placeholder="$t('page.todo.dueDate')"
        size="small"
        @change="setDueDate"
      >
        <template #renderExtraFooter>
          <div class="flex gap-2 py-1">
            <button
              v-for="p in dueQuickPresets()"
              :key="p.label"
              class="cursor-pointer rounded px-2 py-1 text-xs text-primary transition-colors hover:bg-accent"
              type="button"
              @click="setDueDate(p.value())"
            >
              {{ p.label }}
            </button>
          </div>
        </template>
      </DatePicker>

      <!-- 計畫日 -->
      <DatePicker
        :value="scheduledDateValue"
        format="YYYY-MM-DD"
        placeholder="計畫日期"
        size="small"
        @change="setScheduledDate"
      />

      <!-- 專案 -->
      <Popover :overlay-inner-style="{ padding: '6px', width: '220px' }" trigger="click">
        <button
          class="todo-chip cursor-pointer"
          type="button"
        >
          <span
            class="size-2.5 rounded-full"
            :style="{ backgroundColor: currentProjectMeta.color }"
          />
          {{ currentProjectMeta.label }}
        </button>
        <template #content>
          <div class="flex max-h-64 flex-col gap-1 overflow-y-auto">
            <button
              v-for="opt in projectOptions"
              :key="opt.value ?? 'inbox'"
              class="flex cursor-pointer items-center gap-2 rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
              type="button"
              @click="setProject(opt.value)"
            >
              <span
                class="size-2.5 rounded-full"
                :style="{ backgroundColor: opt.color }"
              />
              <span class="flex-1 truncate">{{ opt.label }}</span>
              <svg
                v-if="item.projectId === opt.value"
                class="size-3.5 text-primary"
                fill="none"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.check" />
              </svg>
            </button>
          </div>
        </template>
      </Popover>
    </div>

    <!-- Tags -->
    <div class="mb-3">
      <Select
        :value="tagSelectValue"
        class="w-full"
        mode="multiple"
        placeholder="選擇或輸入標籤"
        size="small"
        @change="(v) => setTags(v as string[])"
      >
        <SelectOption
          v-for="opt in tagOptions"
          :key="opt.value"
          :label="opt.label"
          :value="opt.value"
        >
          <span class="inline-flex items-center gap-1.5">
            <span
              class="size-2 rounded-full"
              :style="{ backgroundColor: opt.color }"
            />
            {{ opt.label }}
          </span>
        </SelectOption>
      </Select>
    </div>

    <!-- Checklist -->
    <div v-if="!isInTrash" class="mb-3">
      <div
        class="mb-1.5 flex items-center justify-between text-xs font-medium text-muted-foreground"
      >
        <span>檢查清單</span>
        <span v-if="checklistProgress.total > 0">
          {{ checklistProgress.done }} / {{ checklistProgress.total }}
        </span>
      </div>
      <div class="space-y-0.5">
        <div
          v-for="ci in item.checklistItems"
          :key="ci.id"
          class="group flex cursor-default items-center gap-2 rounded-md px-2 py-1 hover:bg-accent/60"
        >
          <button
            class="flex size-4 flex-shrink-0 cursor-pointer items-center justify-center rounded border transition-colors"
            :class="
              ci.isCompleted
                ? 'border-green-500 bg-green-500'
                : 'border-border hover:border-primary'
            "
            type="button"
            @click="store.toggleChecklistItem(item.id, ci)"
          >
            <svg
              v-if="ci.isCompleted"
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
          <span
            class="flex-1 text-sm"
            :class="
              ci.isCompleted
                ? 'text-muted-foreground line-through'
                : 'text-foreground'
            "
          >
            {{ ci.title }}
          </span>
          <button
            class="hidden cursor-pointer text-xs text-muted-foreground transition-colors hover:text-red-500 group-hover:block"
            type="button"
            @click="store.deleteChecklistItem(item.id, ci.id)"
          >
            ✕
          </button>
        </div>
      </div>
      <div class="mt-2 flex gap-2">
        <input
          v-model="newChecklistTitle"
          class="flex-1 rounded-md border border-border bg-transparent px-2.5 py-1 text-sm text-foreground placeholder:text-muted-foreground focus:border-primary focus:outline-none"
          placeholder="新增子項目…"
          @keydown.enter.prevent="addChecklist"
          @keydown.esc.stop
        />
        <button
          class="cursor-pointer rounded-md bg-primary px-3 py-1 text-xs font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
          :disabled="!newChecklistTitle.trim()"
          type="button"
          @click="addChecklist"
        >
          新增
        </button>
      </div>
    </div>

    <!-- 子任務（read-only）-->
    <div v-if="item.children.length > 0" class="mb-3">
      <div class="mb-1.5 text-xs font-medium text-muted-foreground">
        子任務 ({{ item.children.length }})
      </div>
      <div class="space-y-0.5">
        <button
          v-for="child in item.children"
          :key="child.id"
          class="flex w-full cursor-pointer items-center gap-2 rounded-md px-2 py-1 text-left text-sm hover:bg-accent/60"
          type="button"
          @click="store.selectItem(child.id)"
        >
          <span
            class="size-2.5 rounded-full border-2"
            :class="
              child.status === 'Done'
                ? 'border-green-500 bg-green-500'
                : 'border-border'
            "
          />
          <span
            :class="
              child.status === 'Done'
                ? 'text-muted-foreground line-through'
                : 'text-foreground'
            "
          >
            {{ child.title }}
          </span>
        </button>
      </div>
    </div>

    <!-- Footer -->
    <div
      class="flex flex-wrap items-center justify-between gap-2 border-t border-border/60 pt-3 text-xs"
    >
      <div class="text-muted-foreground">
        建立於 {{ new Date(item.createdAt).toLocaleDateString('zh-TW') }}
        <span v-if="item.completedAt" class="ml-2">
          · 完成於 {{ new Date(item.completedAt).toLocaleDateString('zh-TW') }}
        </span>
        <span v-if="item.archivedAt" class="ml-2 text-amber-600">
          · 已封存
        </span>
      </div>
      <div class="flex items-center gap-1">
        <template v-if="isInTrash">
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 font-medium text-blue-600 transition-colors hover:bg-blue-500/10"
            type="button"
            @click="handleRestore"
          >
            還原
          </button>
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 font-medium text-red-500 transition-colors hover:bg-red-500/10"
            type="button"
            @click="confirmPermanentDelete"
          >
            永久刪除
          </button>
        </template>
        <template v-else>
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            type="button"
            @click="handleArchive"
          >
            {{ item.archivedAt ? '取消封存' : '封存' }}
          </button>
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 font-medium text-red-500 transition-colors hover:bg-red-500/10"
            type="button"
            @click="confirmDelete"
          >
            {{ $t('page.todo.delete') }}
          </button>
        </template>
      </div>
    </div>
  </div>
</template>
