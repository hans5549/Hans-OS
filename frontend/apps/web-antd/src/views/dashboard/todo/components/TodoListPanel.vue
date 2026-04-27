<script setup lang="ts">
import type { TodoItem, TodoProject, TodoTag } from '#/api/core/todos';

import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

import { DatePicker, message, Modal, Popover, Select, SelectOption } from 'ant-design-vue';
import type { Dayjs } from 'dayjs';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import { useTodoSelection } from '../composables/useTodoSelection';
import { todoIcons } from '../composables/useTodoMeta';
import TodoAddForm from './TodoAddForm.vue';
import TodoEmptyState from './TodoEmptyState.vue';
import TodoItemRow from './TodoItemRow.vue';

const store = useTodoStore();
const selection = useTodoSelection();

const addFormRef = ref<InstanceType<typeof TodoAddForm> | null>(null);
const showCompleted = ref(false);
const searchText = ref('');
let searchTimer: ReturnType<typeof setTimeout> | null = null;

// ── Title / Empty Hint ─────────────────────────────
const viewTitle = computed(() => {
  const v = store.currentView;
  if (v === 'today') return $t('page.todo.today');
  if (v === 'inbox') return $t('page.todo.inbox');
  if (v === 'upcoming') return $t('page.todo.upcoming');
  if (v === 'all') return $t('page.todo.all');
  if (v === 'trash') return $t('page.todo.trash');
  if (v === 'project') {
    const p = store.projects.find((x: TodoProject) => x.id === store.currentProjectId);
    return p?.name ?? '專案';
  }
  if (v === 'tag') {
    const t = store.tags.find((x: TodoTag) => x.id === store.currentTagId);
    return t ? `#${t.name}` : '標籤';
  }
  if (v === 'search') return `搜尋 "${store.searchQuery}"`;
  return '';
});

// ── Items ──────────────────────────────────────────
const isInTrash = computed(() => store.currentView === 'trash');
const isSearching = computed(() => store.currentView === 'search');

const allItems = computed<TodoItem[]>(() => {
  if (isInTrash.value) return store.trashItems;
  if (isSearching.value) return store.searchResults;
  return store.items;
});

const pendingItems = computed(() =>
  allItems.value.filter((i) => i.status !== 'Done'),
);
const completedItems = computed(() =>
  allItems.value.filter((i) => i.status === 'Done'),
);

// 同步 selection.orderedIds（用於 shift+select）
watch(
  pendingItems,
  (list) => {
    selection.setOrderedIds(list.map((i) => i.id));
  },
  { immediate: true },
);

// ── Search ─────────────────────────────────────────
function handleSearch(q: string) {
  searchText.value = q;
  if (searchTimer !== null) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => {
    if (q.trim()) {
      store.currentView = 'search';
      store.search(q);
    } else if (store.currentView === 'search') {
      store.setView('today');
    }
  }, 300);
}

// ── Today Progress ─────────────────────────────────
const showProgress = computed(
  () => store.currentView === 'today' && store.todayProgress.total > 0,
);
const progressPercent = computed(() => {
  const { done, total } = store.todayProgress;
  return total === 0 ? 0 : Math.round((done / total) * 100);
});

// ── Add Form Trigger ───────────────────────────────
function handleAddEvent() {
  addFormRef.value?.expand();
}
onMounted(() => {
  document.addEventListener('todo:add', handleAddEvent);
});
onUnmounted(() => {
  document.removeEventListener('todo:add', handleAddEvent);
  if (searchTimer !== null) clearTimeout(searchTimer);
});

// ── Batch Actions ──────────────────────────────────
const showBatchProject = ref(false);
const showBatchTag = ref(false);

async function batchComplete() {
  const ids = [...selection.selected.value];
  await store.batchUpdate(ids, 'Done');
  selection.clear();
  message.success(`已將 ${ids.length} 個項目標記為完成`);
}

async function batchSetDueDate(d: Dayjs | null | string) {
  if (!d || typeof d === 'string') return;
  const ids = [...selection.selected.value];
  const dueDate = d.format('YYYY-MM-DD');
  // 個別更新（API 沒提供批次 due date）
  await Promise.all(
    ids.map(async (id) => {
      const item = store.items.find((i: TodoItem) => i.id === id);
      if (!item) return;
      await store.updateItem(id, {
        description: null,
        difficulty: item.difficulty,
        dueDate,
        priority: item.priority,
        projectId: item.projectId,
        scheduledDate: item.scheduledDate,
        status: item.status,
        tagIds: item.tags.map((t: TodoTag) => t.id),
        title: item.title,
      });
    }),
  );
  selection.clear();
  message.success(`已設定 ${ids.length} 個項目的截止日`);
}

async function batchSetProject(pid: null | string) {
  const ids = [...selection.selected.value];
  await Promise.all(
    ids.map(async (id) => {
      const item = store.items.find((i: TodoItem) => i.id === id);
      if (!item) return;
      await store.updateItem(id, {
        description: null,
        difficulty: item.difficulty,
        dueDate: item.dueDate,
        priority: item.priority,
        projectId: pid,
        scheduledDate: item.scheduledDate,
        status: item.status,
        tagIds: item.tags.map((t: TodoTag) => t.id),
        title: item.title,
      });
    }),
  );
  selection.clear();
  showBatchProject.value = false;
  message.success(`已移動 ${ids.length} 個項目`);
}

async function batchAddTag(tagIds: string[]) {
  if (tagIds.length === 0) return;
  const ids = [...selection.selected.value];
  await Promise.all(
    ids.map(async (id) => {
      const item = store.items.find((i: TodoItem) => i.id === id);
      if (!item) return;
      const merged = Array.from(new Set([...item.tags.map((t: TodoTag) => t.id), ...tagIds]));
      await store.updateItem(id, {
        description: null,
        difficulty: item.difficulty,
        dueDate: item.dueDate,
        priority: item.priority,
        projectId: item.projectId,
        scheduledDate: item.scheduledDate,
        status: item.status,
        tagIds: merged,
        title: item.title,
      });
    }),
  );
  selection.clear();
  showBatchTag.value = false;
  message.success(`已加上 ${tagIds.length} 個標籤`);
}

function batchDelete() {
  const ids = [...selection.selected.value];
  Modal.confirm({
    cancelText: $t('page.todo.cancel'),
    okText: $t('page.todo.delete'),
    okType: 'danger',
    onOk: async () => {
      await Promise.all(ids.map((id) => store.deleteItem(id)));
      selection.clear();
      message.success(`已移到垃圾桶（${ids.length}）`);
    },
    title: `將 ${ids.length} 個項目移到垃圾桶？`,
  });
}

const projectOptions = computed(() => [
  { value: null as null | string, label: 'Inbox' },
  ...store.projects
    .filter((p: TodoProject) => !p.isArchived)
    .map((p: TodoProject) => ({ value: p.id, label: p.name })),
]);

const tagSelectOptions = computed(() =>
  store.tags.map((t: TodoTag) => ({ value: t.id, label: t.name })),
);
</script>

<template>
  <main class="flex h-full min-w-0 flex-col">
    <!-- Header -->
    <header
      class="flex flex-shrink-0 items-center gap-3 border-b border-border/60 px-6 py-3"
    >
      <h1 class="truncate text-lg font-semibold text-foreground">
        {{ viewTitle }}
      </h1>
      <span class="text-sm text-muted-foreground">
        {{ pendingItems.length }}
      </span>
      <div class="ml-auto flex items-center gap-2">
        <a-input
          v-model:value="searchText"
          allow-clear
          class="!w-56"
          :placeholder="$t('page.todo.searchPlaceholder')"
          size="small"
          @update:value="handleSearch"
        >
          <template #prefix>
            <svg
              class="size-3.5 text-muted-foreground"
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              viewBox="0 0 24 24"
            >
              <path :d="todoIcons.search" />
            </svg>
          </template>
        </a-input>
      </div>
    </header>

    <!-- Today Progress -->
    <div v-if="showProgress" class="flex-shrink-0 px-6 pt-3">
      <div class="flex items-center gap-3 text-xs text-muted-foreground">
        <span>今日進度</span>
        <div class="relative h-1.5 flex-1 overflow-hidden rounded-full bg-muted">
          <div
            class="absolute inset-y-0 left-0 rounded-full bg-primary transition-all duration-300"
            :style="{ width: `${progressPercent}%` }"
          />
        </div>
        <span class="font-medium text-foreground">
          {{ store.todayProgress.done }} / {{ store.todayProgress.total }}
        </span>
      </div>
    </div>

    <!-- Batch Toolbar -->
    <Transition name="fade">
      <div
        v-if="selection.hasSelection.value"
        class="mx-6 mt-3 flex flex-shrink-0 flex-wrap items-center gap-2 rounded-lg border border-primary/30 bg-primary/5 px-3 py-2 text-sm"
      >
        <span class="font-medium text-primary">
          已選取 {{ selection.count.value }}
        </span>
        <button
          class="cursor-pointer rounded-md px-2 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent"
          type="button"
          @click="selection.clear()"
        >
          取消
        </button>
        <div class="ml-2 flex flex-wrap items-center gap-1.5">
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium text-foreground transition-colors hover:bg-accent"
            type="button"
            @click="batchComplete"
          >
            ✓ 標完成
          </button>
          <DatePicker
            format="YYYY-MM-DD"
            placeholder="設截止日"
            size="small"
            @change="(v) => batchSetDueDate(v as Dayjs | null | string)"
          />
          <Popover
            v-model:open="showBatchProject"
            :overlay-inner-style="{ padding: '6px', width: '220px' }"
            trigger="click"
          >
            <button
              class="cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium text-foreground transition-colors hover:bg-accent"
              type="button"
            >
              改專案
            </button>
            <template #content>
              <div class="flex max-h-64 flex-col gap-1 overflow-y-auto">
                <button
                  v-for="opt in projectOptions"
                  :key="opt.value ?? 'inbox'"
                  class="cursor-pointer rounded px-2.5 py-1.5 text-left text-sm transition-colors hover:bg-accent"
                  type="button"
                  @click="batchSetProject(opt.value)"
                >
                  {{ opt.label }}
                </button>
              </div>
            </template>
          </Popover>
          <Popover
            v-model:open="showBatchTag"
            :overlay-inner-style="{ padding: '8px', width: '240px' }"
            trigger="click"
          >
            <button
              class="cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium text-foreground transition-colors hover:bg-accent"
              type="button"
            >
              加標籤
            </button>
            <template #content>
              <Select
                allow-clear
                class="w-full"
                mode="multiple"
                placeholder="選擇標籤"
                size="small"
                @change="(v) => batchAddTag(v as string[])"
              >
                <SelectOption
                  v-for="opt in tagSelectOptions"
                  :key="opt.value"
                  :value="opt.value"
                >
                  {{ opt.label }}
                </SelectOption>
              </Select>
            </template>
          </Popover>
          <button
            class="cursor-pointer rounded-md px-2.5 py-1 text-xs font-medium text-red-500 transition-colors hover:bg-red-500/10"
            type="button"
            @click="batchDelete"
          >
            移到垃圾桶
          </button>
        </div>
      </div>
    </Transition>

    <!-- Add form -->
    <div
      v-if="!isInTrash && !isSearching"
      class="flex-shrink-0 px-6 py-3"
    >
      <TodoAddForm ref="addFormRef" />
    </div>

    <!-- List -->
    <div class="flex-1 overflow-y-auto px-3 pb-6">
      <div v-if="store.loading.items" class="px-6 py-8 text-sm text-muted-foreground">
        載入中…
      </div>

      <template v-else-if="pendingItems.length === 0 && completedItems.length === 0">
        <TodoEmptyState
          v-if="isInTrash"
          title="垃圾桶是空的"
          :icon-path="todoIcons.trash"
        />
        <TodoEmptyState v-else title="沒有任務" description="按 N 鍵新增任務" />
      </template>

      <template v-else>
        <div class="px-3">
          <TodoItemRow
            v-for="item in pendingItems"
            :key="item.id"
            :item="item"
          />
        </div>

        <!-- Completed -->
        <details
          v-if="completedItems.length > 0"
          class="group mt-4 px-3"
        >
          <summary
            class="flex cursor-pointer list-none items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            @click.prevent="showCompleted = !showCompleted"
          >
            <svg
              class="size-3 transition-transform"
              :class="showCompleted ? 'rotate-0' : '-rotate-90'"
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2.5"
              viewBox="0 0 24 24"
            >
              <path :d="todoIcons.chevronDown" />
            </svg>
            已完成 ({{ completedItems.length }})
          </summary>
          <div v-if="showCompleted" class="mt-1">
            <TodoItemRow
              v-for="item in completedItems"
              :key="item.id"
              :item="item"
            />
          </div>
        </details>
      </template>
    </div>
  </main>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
