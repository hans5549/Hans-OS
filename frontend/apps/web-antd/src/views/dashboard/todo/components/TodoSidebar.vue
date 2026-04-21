<script setup lang="ts">
import { ref } from 'vue';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import TodoProjectModal from './TodoProjectModal.vue';

const store = useTodoStore();
const isProjectModalOpen = ref(false);

type SmartFilter = {
  countKey: 'all' | 'inbox' | 'today' | 'upcoming';
  iconPath: string;
  label: string;
  view: 'all' | 'inbox' | 'today' | 'upcoming';
};

const smartFilters: SmartFilter[] = [
  {
    view: 'inbox',
    label: 'page.todo.inbox',
    iconPath: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    countKey: 'inbox',
  },
  {
    view: 'today',
    label: 'page.todo.today',
    iconPath: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z',
    countKey: 'today',
  },
  {
    view: 'upcoming',
    label: 'page.todo.upcoming',
    iconPath: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    countKey: 'upcoming',
  },
  {
    view: 'all',
    label: 'page.todo.all',
    iconPath: 'M4 6h16M4 10h16M4 14h16M4 18h16',
    countKey: 'all',
  },
];

function handleFilterClick(view: 'all' | 'inbox' | 'today' | 'upcoming') {
  store.setView(view);
}

function handleProjectClick(projectId: string) {
  store.setView('project', projectId);
}
</script>

<template>
  <aside class="flex h-full w-56 flex-shrink-0 flex-col border-r border-slate-200 bg-white">
    <div class="flex-1 overflow-y-auto p-3">
      <!-- Smart Filters -->
      <div class="mb-4">
        <button
          v-for="filter in smartFilters"
          :key="filter.view"
          class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors"
          :class="
            store.currentView === filter.view
              ? 'bg-blue-50 font-semibold text-blue-700'
              : 'text-slate-700 hover:bg-slate-100'
          "
          type="button"
          @click="handleFilterClick(filter.view)"
        >
          <svg class="size-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path :d="filter.iconPath" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
          </svg>
          <span class="flex-1 text-left">{{ $t(filter.label) }}</span>
          <span
            v-if="store.counts[filter.countKey] > 0"
            class="min-w-[20px] rounded-full bg-slate-200 px-1.5 text-center text-xs font-medium text-slate-600"
            :class="store.currentView === filter.view ? 'bg-blue-100 text-blue-600' : ''"
          >
            {{ store.counts[filter.countKey] }}
          </span>
        </button>
      </div>

      <!-- Divider -->
      <div class="mb-2 flex items-center gap-2 px-2">
        <span class="text-xs font-semibold uppercase tracking-wide text-slate-400">專案</span>
      </div>

      <!-- Projects -->
      <button
        v-for="project in store.projects.filter((p) => !p.isArchived)"
        :key="project.id"
        class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors"
        :class="
          store.currentView === 'project' && store.currentProjectId === project.id
            ? 'bg-blue-50 font-semibold text-blue-700'
            : 'text-slate-700 hover:bg-slate-100'
        "
        type="button"
        @click="handleProjectClick(project.id)"
      >
        <span
          class="size-3 flex-shrink-0 rounded-full"
          :style="{ backgroundColor: project.color }"
        />
        <span class="flex-1 truncate text-left">{{ project.name }}</span>
        <span
          v-if="project.itemCount > 0"
          class="min-w-[20px] rounded-full bg-slate-200 px-1.5 text-center text-xs font-medium text-slate-600"
        >
          {{ project.itemCount }}
        </span>
      </button>
    </div>

    <!-- Add Project button -->
    <div class="border-t border-slate-200 p-3">
      <button
        class="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-700"
        type="button"
        @click="isProjectModalOpen = true"
      >
        <svg class="size-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path d="M12 5v14m-7-7h14" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
        </svg>
        {{ $t('page.todo.addProject') }}
      </button>
    </div>

    <TodoProjectModal v-model:open="isProjectModalOpen" />
  </aside>
</template>
