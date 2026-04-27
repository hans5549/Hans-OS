<script setup lang="ts">
import type { TodoProject } from '#/api/core/todos';

import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';

import { Dropdown, Menu, MenuItem, Tooltip } from 'ant-design-vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import { todoIcons } from '../composables/useTodoMeta';
import TodoProjectModal from './TodoProjectModal.vue';

const store = useTodoStore();
const router = useRouter();

const projectModalOpen = ref(false);
const editingProject = ref<null | TodoProject>(null);
const tagsExpanded = ref(true);
const projectsExpanded = ref(true);

type SmartFilter = {
  countKey?: 'all' | 'inbox' | 'today' | 'upcoming';
  iconPath: string;
  label: string;
  view: 'all' | 'inbox' | 'today' | 'trash' | 'upcoming' | 'week';
};

const smartFilters: SmartFilter[] = [
  {
    countKey: 'inbox',
    iconPath: todoIcons.inbox,
    label: $t('page.todo.inbox'),
    view: 'inbox',
  },
  {
    countKey: 'today',
    iconPath: todoIcons.today,
    label: $t('page.todo.today'),
    view: 'today',
  },
  {
    countKey: 'upcoming',
    iconPath: todoIcons.upcoming,
    label: $t('page.todo.upcoming'),
    view: 'upcoming',
  },
  {
    iconPath: todoIcons.week,
    label: '週曆',
    view: 'week',
  },
  {
    countKey: 'all',
    iconPath: todoIcons.all,
    label: $t('page.todo.all'),
    view: 'all',
  },
];

const trashFilter: SmartFilter = {
  iconPath: todoIcons.trash,
  label: $t('page.todo.trash'),
  view: 'trash',
};

const activeProjects = computed(() => store.projects.filter((p: TodoProject) => !p.isArchived));

function go(view: SmartFilter['view']) {
  router.replace({ path: '/todo', query: { view } });
}

function goProject(projectId: string) {
  router.replace({ path: '/todo', query: { id: projectId, view: 'project' } });
}

function goTag(tagId: string) {
  router.replace({ path: '/todo', query: { id: tagId, view: 'tag' } });
}

function newProject() {
  editingProject.value = null;
  projectModalOpen.value = true;
}

function editProject(project: TodoProject) {
  editingProject.value = project;
  projectModalOpen.value = true;
}

function isFilterActive(view: SmartFilter['view']) {
  return store.currentView === view;
}

function isProjectActive(projectId: string) {
  return store.currentView === 'project' && store.currentProjectId === projectId;
}
</script>

<template>
  <aside
    class="flex h-full w-60 flex-shrink-0 flex-col border-r border-border bg-background/70 backdrop-blur-xl"
  >
    <div class="flex-1 overflow-y-auto px-3 py-4">
      <!-- ── Smart Filters ────────────────────── -->
      <nav class="space-y-0.5">
        <button
          v-for="f in smartFilters"
          :key="f.view"
          class="group flex w-full cursor-pointer items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors duration-150"
          :class="
            isFilterActive(f.view)
              ? 'bg-primary/10 font-semibold text-primary'
              : 'text-foreground hover:bg-accent'
          "
          type="button"
          @click="go(f.view)"
        >
          <svg
            class="size-[18px] flex-shrink-0"
            fill="none"
            stroke="currentColor"
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="1.8"
            viewBox="0 0 24 24"
          >
            <path :d="f.iconPath" />
          </svg>
          <span class="flex-1 text-left">{{ f.label }}</span>
          <span
            v-if="f.countKey && store.counts[f.countKey] > 0"
            class="min-w-[22px] rounded-md bg-muted px-1.5 text-center text-xs font-medium text-muted-foreground"
            :class="
              isFilterActive(f.view) ? 'bg-primary/15 text-primary' : ''
            "
          >
            {{ store.counts[f.countKey] }}
          </span>
        </button>
      </nav>

      <!-- ── Projects ─────────────────────────── -->
      <div class="mt-6">
        <div class="mb-2 flex items-center px-2">
          <button
            class="flex flex-1 cursor-pointer items-center gap-1.5 text-left"
            type="button"
            @click="projectsExpanded = !projectsExpanded"
          >
            <svg
              class="size-3 text-muted-foreground transition-transform"
              :class="projectsExpanded ? '' : '-rotate-90'"
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2.5"
              viewBox="0 0 24 24"
            >
              <path :d="todoIcons.chevronDown" />
            </svg>
            <span
              class="text-xs font-semibold uppercase tracking-wider text-muted-foreground"
            >
              專案
            </span>
          </button>
          <Tooltip placement="right" title="新增專案">
            <button
              class="flex size-5 cursor-pointer items-center justify-center rounded text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              type="button"
              @click="newProject"
            >
              <svg
                class="size-3.5"
                fill="none"
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                viewBox="0 0 24 24"
              >
                <path :d="todoIcons.plus" />
              </svg>
            </button>
          </Tooltip>
        </div>

        <div v-if="projectsExpanded" class="space-y-0.5">
          <Dropdown
            v-for="project in activeProjects"
            :key="project.id"
            :trigger="['contextmenu']"
          >
            <button
              class="group flex w-full cursor-pointer items-center gap-2.5 rounded-lg px-3 py-1.5 text-sm transition-colors duration-150"
              :class="
                isProjectActive(project.id)
                  ? 'bg-primary/10 font-semibold text-primary'
                  : 'text-foreground hover:bg-accent'
              "
              type="button"
              @click="goProject(project.id)"
            >
              <span
                class="size-2.5 flex-shrink-0 rounded-full"
                :style="{ backgroundColor: project.color }"
              />
              <span class="flex-1 truncate text-left">{{ project.name }}</span>
              <span
                v-if="project.itemCount > 0"
                class="text-xs text-muted-foreground"
              >
                {{ project.itemCount }}
              </span>
            </button>
            <template #overlay>
              <Menu>
                <MenuItem key="edit" @click="editProject(project)">
                  編輯
                </MenuItem>
              </Menu>
            </template>
          </Dropdown>

          <button
            v-if="activeProjects.length === 0"
            class="flex w-full cursor-pointer items-center gap-2 rounded-lg px-3 py-1.5 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            type="button"
            @click="newProject"
          >
            <svg
              class="size-3.5"
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              viewBox="0 0 24 24"
            >
              <path :d="todoIcons.plus" />
            </svg>
            建立第一個專案
          </button>
        </div>
      </div>

      <!-- ── Tags ─────────────────────────────── -->
      <div v-if="store.tags.length > 0" class="mt-6">
        <button
          class="mb-2 flex w-full cursor-pointer items-center gap-1.5 px-2 text-left"
          type="button"
          @click="tagsExpanded = !tagsExpanded"
        >
          <svg
            class="size-3 text-muted-foreground transition-transform"
            :class="tagsExpanded ? '' : '-rotate-90'"
            fill="none"
            stroke="currentColor"
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2.5"
            viewBox="0 0 24 24"
          >
            <path :d="todoIcons.chevronDown" />
          </svg>
          <span
            class="text-xs font-semibold uppercase tracking-wider text-muted-foreground"
          >
            標籤
          </span>
        </button>
        <div v-if="tagsExpanded" class="space-y-0.5">
          <button
            v-for="tag in store.tags"
            :key="tag.id"
            class="flex w-full cursor-pointer items-center gap-2.5 rounded-lg px-3 py-1.5 text-sm transition-colors duration-150"
            :class="
              store.currentTagId === tag.id
                ? 'bg-primary/10 font-semibold text-primary'
                : 'text-foreground hover:bg-accent'
            "
            type="button"
            @click="goTag(tag.id)"
          >
            <span
              class="size-2 flex-shrink-0 rounded-full"
              :style="{ backgroundColor: tag.color ?? '#94A3B8' }"
            />
            <span class="flex-1 truncate text-left">{{ tag.name }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ── Footer：Trash ──────────────────────── -->
    <div class="border-t border-border px-3 py-2">
      <button
        class="flex w-full cursor-pointer items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors duration-150"
        :class="
          isFilterActive('trash')
            ? 'bg-red-500/10 font-semibold text-red-500'
            : 'text-muted-foreground hover:bg-accent hover:text-foreground'
        "
        type="button"
        @click="go('trash')"
      >
        <svg
          class="size-[18px] flex-shrink-0"
          fill="none"
          stroke="currentColor"
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="1.8"
          viewBox="0 0 24 24"
        >
          <path :d="trashFilter.iconPath" />
        </svg>
        {{ trashFilter.label }}
      </button>
    </div>

    <TodoProjectModal v-model:open="projectModalOpen" :project="editingProject" />
  </aside>
</template>
