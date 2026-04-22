<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

import { $t } from '#/locales';
import { useTodoStore } from '#/store/todo';

import TodoProjectModal from './TodoProjectModal.vue';

const store = useTodoStore();
const router = useRouter();
const isProjectModalOpen = ref(false);
const isTagsExpanded = ref(false);

type SmartFilter = {
  countKey?: 'all' | 'inbox' | 'today' | 'upcoming';
  iconPath: string;
  label: string;
  path: string;
  view: 'all' | 'inbox' | 'today' | 'trash' | 'upcoming' | 'week';
};

const smartFilters: SmartFilter[] = [
  {
    countKey: 'inbox',
    iconPath: 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    label: 'page.todo.inbox',
    path: '/todo/inbox',
    view: 'inbox',
  },
  {
    countKey: 'today',
    iconPath: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z',
    label: 'page.todo.today',
    path: '/todo/today',
    view: 'today',
  },
  {
    countKey: 'upcoming',
    iconPath: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    label: 'page.todo.upcoming',
    path: '/todo/upcoming',
    view: 'upcoming',
  },
  {
    iconPath: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
    label: 'page.todo.week',
    path: '/todo/week',
    view: 'week',
  },
  {
    countKey: 'all',
    iconPath: 'M4 6h16M4 10h16M4 14h16M4 18h16',
    label: 'page.todo.all',
    path: '/todo/all',
    view: 'all',
  },
  {
    iconPath: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16',
    label: 'page.todo.trash',
    path: '/todo/trash',
    view: 'trash',
  },
];

function handleFilterClick(filter: SmartFilter) {
  router.push(filter.path);
}

function handleProjectClick(projectId: string) {
  router.push(`/todo/project/${projectId}`);
}

function handleTagClick(tagId: string) {
  router.push(`/todo/tag/${tagId}`);
}
</script>

<template>
  <aside class="flex h-full w-56 flex-shrink-0 flex-col border-r border-border bg-background">
    <div class="flex-1 overflow-y-auto p-3">
      <!-- Smart Filters -->
      <div class="mb-4">
        <button
          v-for="filter in smartFilters"
          :key="filter.view"
          class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors"
          :class="[
            store.currentView === filter.view
              ? 'bg-primary/10 font-semibold text-primary'
              : 'text-foreground hover:bg-accent',
            filter.view === 'trash' ? 'mt-1 text-red-500/70 hover:text-red-500' : '',
          ]"
          type="button"
          @click="handleFilterClick(filter)"
        >
          <svg class="size-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path :d="filter.iconPath" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
          </svg>
          <span class="flex-1 text-left">{{ $t(filter.label) }}</span>
          <span
            v-if="filter.countKey && store.counts[filter.countKey] > 0"
            class="min-w-[20px] rounded-full bg-accent px-1.5 text-center text-xs font-medium text-muted-foreground"
            :class="store.currentView === filter.view ? 'bg-primary/15 text-primary' : ''"
          >
            {{ store.counts[filter.countKey] }}
          </span>
        </button>
      </div>

      <!-- Projects -->
      <div class="mb-2 flex items-center gap-2 px-2">
        <span class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">專案</span>
      </div>

      <button
        v-for="project in store.projects.filter((p) => !p.isArchived)"
        :key="project.id"
        class="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm transition-colors"
        :class="
          store.currentView === 'project' && store.currentProjectId === project.id
            ? 'bg-primary/10 font-semibold text-primary'
            : 'text-foreground hover:bg-accent'
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
          class="min-w-[20px] rounded-full bg-accent px-1.5 text-center text-xs font-medium text-muted-foreground"
        >
          {{ project.itemCount }}
        </span>
      </button>

      <!-- Tags section -->
      <div v-if="store.tags.length > 0" class="mt-4">
        <button
          class="mb-1 flex w-full items-center gap-2 px-2"
          type="button"
          @click="isTagsExpanded = !isTagsExpanded"
        >
          <span class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">標籤</span>
          <svg
            class="ml-auto size-3 text-muted-foreground transition-transform"
            :class="isTagsExpanded ? 'rotate-180' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M19 9l-7 7-7-7" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" />
          </svg>
        </button>
        <div v-if="isTagsExpanded">
          <button
            v-for="tag in store.tags"
            :key="tag.id"
            class="flex w-full items-center gap-2.5 rounded-lg px-3 py-1.5 text-sm transition-colors hover:bg-accent"
            :class="store.currentTagId === tag.id ? 'bg-primary/10 font-semibold text-primary' : 'text-foreground'"
            type="button"
            @click="handleTagClick(tag.id)"
          >
            <span
              class="size-2.5 flex-shrink-0 rounded-full"
              :style="{ backgroundColor: tag.color ?? '#64748B' }"
            />
            <span class="flex-1 truncate text-left">{{ tag.name }}</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Add Project button -->
    <div class="border-t border-border p-3">
      <button
        class="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
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
