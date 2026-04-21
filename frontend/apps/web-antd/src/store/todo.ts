import { computed, ref } from 'vue';

import { defineStore } from 'pinia';

import type {
  CreateItemRequest,
  CreateProjectRequest,
  TodoCounts,
  TodoItem,
  TodoProject,
  TodoViewFilter,
  UpdateItemRequest,
  UpdateProjectRequest,
} from '#/api/core/todos';
import {
  createItemApi,
  createProjectApi,
  deleteItemApi,
  deleteProjectApi,
  getCountsApi,
  getItemsApi,
  getProjectsApi,
  toggleCompleteApi,
  updateItemApi,
  updateProjectApi,
} from '#/api/core/todos';

export const useTodoStore = defineStore('todo', () => {
  // ── State ──────────────────────────────────────────

  const projects = ref<TodoProject[]>([]);
  const items = ref<TodoItem[]>([]);
  const counts = ref<TodoCounts>({ inbox: 0, today: 0, upcoming: 0, all: 0 });

  const selectedItemId = ref<null | string>(null);
  const currentView = ref<'all' | 'project' | TodoViewFilter>('today');
  const currentProjectId = ref<null | string>(null);

  const loading = ref({
    projects: false,
    items: false,
    saving: false,
  });

  // ── Getters ────────────────────────────────────────

  const selectedItem = computed(() =>
    selectedItemId.value
      ? items.value.find((i) => i.id === selectedItemId.value) ?? null
      : null,
  );

  const isDetailOpen = computed(() => selectedItemId.value !== null);

  const pendingItems = computed(() =>
    items.value.filter((i) => i.status !== 'Done'),
  );

  const completedItems = computed(() =>
    items.value.filter((i) => i.status === 'Done'),
  );

  const todayProgress = computed(() => {
    const todayStr = new Date().toISOString().slice(0, 10);
    const todayItems = items.value.filter((i) => i.dueDate === todayStr);
    const done = todayItems.filter((i) => i.status === 'Done').length;
    return { total: todayItems.length, done };
  });

  // ── Project Actions ────────────────────────────────

  async function fetchProjects() {
    loading.value.projects = true;
    try {
      projects.value = await getProjectsApi();
    } finally {
      loading.value.projects = false;
    }
  }

  async function createProject(data: CreateProjectRequest) {
    loading.value.saving = true;
    try {
      const project = await createProjectApi(data);
      projects.value.push(project);
      return project;
    } finally {
      loading.value.saving = false;
    }
  }

  async function updateProject(id: string, data: UpdateProjectRequest) {
    loading.value.saving = true;
    try {
      const updated = await updateProjectApi(id, data);
      const idx = projects.value.findIndex((p) => p.id === id);
      if (idx !== -1) {
        projects.value[idx] = updated;
      }
      return updated;
    } finally {
      loading.value.saving = false;
    }
  }

  async function deleteProject(id: string) {
    await deleteProjectApi(id);
    projects.value = projects.value.filter((p) => p.id !== id);
    if (currentProjectId.value === id) {
      setView('inbox');
    }
  }

  // ── Item Actions ───────────────────────────────────

  async function fetchItems(
    view?: TodoViewFilter,
    projectId?: string,
  ) {
    loading.value.items = true;
    try {
      items.value = await getItemsApi({ view, projectId });
    } finally {
      loading.value.items = false;
    }
  }

  async function fetchCounts() {
    counts.value = await getCountsApi();
  }

  async function createItem(data: CreateItemRequest) {
    loading.value.saving = true;
    try {
      const item = await createItemApi(data);
      items.value.push(item);
      await fetchCounts();
      return item;
    } finally {
      loading.value.saving = false;
    }
  }

  async function updateItem(id: string, data: UpdateItemRequest) {
    loading.value.saving = true;
    try {
      const updated = await updateItemApi(id, data);
      const idx = items.value.findIndex((i) => i.id === id);
      if (idx !== -1) {
        items.value[idx] = updated;
      }
      return updated;
    } finally {
      loading.value.saving = false;
    }
  }

  async function toggleComplete(id: string) {
    // Optimistic update
    const idx = items.value.findIndex((i) => i.id === id);
    if (idx === -1) return;

    const original = items.value[idx];
    const isCurrentlyDone = original?.status === 'Done';
    if (original) {
      items.value[idx] = {
        ...original,
        status: isCurrentlyDone ? 'Pending' : 'Done',
        completedAt: isCurrentlyDone ? null : new Date().toISOString(),
      };
    }

    try {
      const updated = await toggleCompleteApi(id);
      items.value[idx] = updated;
      await fetchCounts();
    } catch {
      // Rollback on error
      if (original) {
        items.value[idx] = original;
      }
    }
  }

  async function deleteItem(id: string) {
    await deleteItemApi(id);
    items.value = items.value.filter((i) => i.id !== id);
    if (selectedItemId.value === id) {
      selectedItemId.value = null;
    }
    await fetchCounts();
  }

  // ── UI State ───────────────────────────────────────

  function setView(view: 'all' | 'project' | TodoViewFilter, projectId?: string) {
    currentView.value = view;
    currentProjectId.value = projectId ?? null;
    selectedItemId.value = null;

    if (view === 'project' && projectId) {
      fetchItems(undefined, projectId);
    } else if (view !== 'project') {
      fetchItems(view as TodoViewFilter);
    }
  }

  function selectItem(id: null | string) {
    selectedItemId.value = id;
  }

  function closeDetail() {
    selectedItemId.value = null;
  }

  // ── Init ───────────────────────────────────────────

  async function init() {
    await Promise.all([fetchProjects(), fetchCounts()]);
    await fetchItems('today');
    currentView.value = 'today';
  }

  return {
    // State
    projects,
    items,
    counts,
    selectedItemId,
    currentView,
    currentProjectId,
    loading,
    // Getters
    selectedItem,
    isDetailOpen,
    pendingItems,
    completedItems,
    todayProgress,
    // Project actions
    fetchProjects,
    createProject,
    updateProject,
    deleteProject,
    // Item actions
    fetchItems,
    fetchCounts,
    createItem,
    updateItem,
    toggleComplete,
    deleteItem,
    // UI state
    setView,
    selectItem,
    closeDetail,
    init,
  };
});
