import { computed, ref } from 'vue';

import { defineStore } from 'pinia';

import type {
  ChecklistItem,
  CreateChecklistItemRequest,
  CreateItemRequest,
  CreateProjectRequest,
  CreateTagRequest,
  TodoCategory,
  TodoCounts,
  TodoItem,
  TodoItemDetail,
  TodoProject,
  TodoTag,
  TodoViewFilter,
  UpdateItemRequest,
  UpdateProjectRequest,
} from '#/api/core/todos';
import {
  addChecklistItemApi,
  archiveItemApi,
  batchUpdateApi,
  createItemApi,
  createProjectApi,
  createTagApi,
  deleteChecklistItemApi,
  deleteItemApi,
  deleteProjectApi,
  deleteTagApi,
  getCategoriesApi,
  getCountsApi,
  getItemDetailApi,
  getItemsApi,
  getProjectsApi,
  getTagsApi,
  getTrashApi,
  permanentDeleteItemApi,
  restoreItemApi,
  searchItemsApi,
  toggleCompleteApi,
  updateChecklistItemApi,
  updateItemApi,
  updateProjectApi,
} from '#/api/core/todos';

export type TodoView = 'all' | 'project' | 'search' | 'trash' | 'week' | TodoViewFilter;

export const useTodoStore = defineStore('todo', () => {
  // ── State ──────────────────────────────────────────

  const projects = ref<TodoProject[]>([]);
  const items = ref<TodoItem[]>([]);
  const trashItems = ref<TodoItem[]>([]);
  const tags = ref<TodoTag[]>([]);
  const categories = ref<TodoCategory[]>([]);
  const counts = ref<TodoCounts>({ inbox: 0, today: 0, upcoming: 0, all: 0 });

  const selectedItemId = ref<null | string>(null);
  const selectedItemDetail = ref<null | TodoItemDetail>(null);
  const currentView = ref<TodoView>('today');
  const currentProjectId = ref<null | string>(null);
  const currentTagId = ref<null | string>(null);
  const searchQuery = ref('');
  const searchResults = ref<TodoItem[]>([]);

  const loading = ref({
    detail: false,
    items: false,
    projects: false,
    saving: false,
    search: false,
  });

  // ── Getters ────────────────────────────────────────

  const selectedItem = computed(() =>
    selectedItemId.value
      ? (selectedItemDetail.value ?? items.value.find((i: TodoItem) => i.id === selectedItemId.value) ?? null)
      : null,
  );

  const isDetailOpen = computed(() => selectedItemId.value !== null);

  const pendingItems = computed(() =>
    items.value.filter((i: TodoItem) => i.status !== 'Done'),
  );

  const completedItems = computed(() =>
    items.value.filter((i: TodoItem) => i.status === 'Done'),
  );

  const todayProgress = computed(() => {
    const todayStr = new Date().toISOString().slice(0, 10);
    const todayItems = items.value.filter(
      (i: TodoItem) => i.dueDate === todayStr || i.scheduledDate === todayStr,
    );
    const done = todayItems.filter((i: TodoItem) => i.status === 'Done').length;
    return { done, total: todayItems.length };
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
      const idx = projects.value.findIndex((p: TodoProject) => p.id === id);
      if (idx !== -1) projects.value[idx] = updated;
      return updated;
    } finally {
      loading.value.saving = false;
    }
  }

  async function deleteProject(id: string) {
    await deleteProjectApi(id);
    projects.value = projects.value.filter((p: TodoProject) => p.id !== id);
    if (currentProjectId.value === id) setView('inbox');
  }

  // ── Item Actions ───────────────────────────────────

  async function fetchItems(view?: TodoViewFilter, projectId?: string, tagId?: string) {
    loading.value.items = true;
    try {
      const res = await getItemsApi({ projectId, tagId, view });
      items.value = res.items;
    } finally {
      loading.value.items = false;
    }
  }

  async function fetchTrash() {
    loading.value.items = true;
    try {
      trashItems.value = await getTrashApi();
    } finally {
      loading.value.items = false;
    }
  }

  async function fetchCounts() {
    counts.value = await getCountsApi();
  }

  async function fetchItemDetail(id: string) {
    loading.value.detail = true;
    try {
      selectedItemDetail.value = await getItemDetailApi(id);
    } finally {
      loading.value.detail = false;
    }
  }

  async function createItem(data: CreateItemRequest) {
    loading.value.saving = true;
    try {
      await createItemApi(data);
      await refreshCurrentView();
      await fetchCounts();
    } finally {
      loading.value.saving = false;
    }
  }

  async function updateItem(id: string, data: UpdateItemRequest) {
    loading.value.saving = true;
    try {
      const updated = await updateItemApi(id, data);
      const idx = items.value.findIndex((i: TodoItem) => i.id === id);
      if (idx !== -1) items.value[idx] = updated;
      // 同步更新 detail
      if (selectedItemDetail.value?.id === id) {
        await fetchItemDetail(id);
      }
      return updated;
    } finally {
      loading.value.saving = false;
    }
  }

  async function toggleComplete(id: string) {
    const idx = items.value.findIndex((i: TodoItem) => i.id === id);
    if (idx === -1) return;

    const original = items.value[idx];
    const isCurrentlyDone = original?.status === 'Done';
    if (original) {
      items.value[idx] = {
        ...original,
        completedAt: isCurrentlyDone ? null : new Date().toISOString(),
        status: isCurrentlyDone ? 'Pending' : 'Done',
      };
    }

    try {
      const updated = await toggleCompleteApi(id);
      items.value[idx] = updated;
      if (selectedItemDetail.value?.id === id) await fetchItemDetail(id);
      await fetchCounts();
    } catch {
      if (original) items.value[idx] = original;
    }
  }

  async function archiveItem(id: string, archive: boolean) {
    loading.value.saving = true;
    try {
      await archiveItemApi(id, archive);
      await refreshCurrentView();
      await fetchCounts();
      if (selectedItemId.value === id) await fetchItemDetail(id);
    } finally {
      loading.value.saving = false;
    }
  }

  async function restoreItem(id: string) {
    await restoreItemApi(id);
    trashItems.value = trashItems.value.filter((i: TodoItem) => i.id !== id);
    if (selectedItemId.value === id) selectedItemId.value = null;
    await fetchCounts();
  }

  async function permanentDeleteItem(id: string) {
    await permanentDeleteItemApi(id);
    trashItems.value = trashItems.value.filter((i: TodoItem) => i.id !== id);
    if (selectedItemId.value === id) selectedItemId.value = null;
  }

  async function deleteItem(id: string) {
    await deleteItemApi(id);
    items.value = items.value.filter((i: TodoItem) => i.id !== id);
    if (selectedItemId.value === id) {
      selectedItemId.value = null;
      selectedItemDetail.value = null;
    }
    await fetchCounts();
  }

  async function batchUpdate(ids: string[], status: 'Done' | 'InProgress' | 'Pending') {
    await batchUpdateApi(ids, status);
    await refreshCurrentView();
    await fetchCounts();
  }

  // ── Checklist Actions ──────────────────────────────

  async function addChecklistItem(itemId: string, data: CreateChecklistItemRequest) {
    const item = await addChecklistItemApi(itemId, data);
    if (selectedItemDetail.value?.id === itemId) {
      selectedItemDetail.value = {
        ...selectedItemDetail.value,
        checklistItems: [...selectedItemDetail.value.checklistItems, item],
      };
    }
    return item;
  }

  async function toggleChecklistItem(itemId: string, checklistItem: ChecklistItem) {
    const updated = await updateChecklistItemApi(itemId, checklistItem.id, {
      isCompleted: !checklistItem.isCompleted,
    });
    if (selectedItemDetail.value?.id === itemId) {
      selectedItemDetail.value = {
        ...selectedItemDetail.value,
        checklistItems: selectedItemDetail.value.checklistItems.map((c: ChecklistItem) =>
          c.id === checklistItem.id ? updated : c,
        ),
      };
    }
  }

  async function deleteChecklistItem(itemId: string, checklistId: string) {
    await deleteChecklistItemApi(itemId, checklistId);
    if (selectedItemDetail.value?.id === itemId) {
      selectedItemDetail.value = {
        ...selectedItemDetail.value,
        checklistItems: selectedItemDetail.value.checklistItems.filter((c: ChecklistItem) => c.id !== checklistId),
      };
    }
  }

  // ── Tags Actions ───────────────────────────────────

  async function fetchTags() {
    tags.value = await getTagsApi();
  }

  async function createTag(data: CreateTagRequest) {
    const tag = await createTagApi(data);
    tags.value.push(tag);
    return tag;
  }

  async function deleteTag(id: string) {
    await deleteTagApi(id);
    tags.value = tags.value.filter((t: TodoTag) => t.id !== id);
  }

  // ── Categories Actions ─────────────────────────────

  async function fetchCategories() {
    categories.value = await getCategoriesApi();
  }

  // ── Search Actions ─────────────────────────────────

  async function search(q: string) {
    searchQuery.value = q;
    if (!q.trim()) {
      searchResults.value = [];
      return;
    }
    loading.value.search = true;
    try {
      searchResults.value = await searchItemsApi(q);
    } finally {
      loading.value.search = false;
    }
  }

  // ── UI State ───────────────────────────────────────

  function refreshCurrentView() {
    if (currentView.value === 'trash') return fetchTrash();
    if (currentView.value === 'project' && currentProjectId.value) {
      return fetchItems(undefined, currentProjectId.value);
    }
    if (currentView.value === 'tag' && currentTagId.value) {
      return fetchItems(undefined, undefined, currentTagId.value);
    }
    if (currentView.value === 'search') return Promise.resolve();
    return fetchItems(currentView.value as TodoViewFilter);
  }

  function setView(view: TodoView, projectId?: string, tagId?: string) {
    currentView.value = view;
    currentProjectId.value = projectId ?? null;
    currentTagId.value = tagId ?? null;
    selectedItemId.value = null;
    selectedItemDetail.value = null;
    searchQuery.value = '';
    searchResults.value = [];

    if (view === 'trash') {
      fetchTrash();
    } else if (view === 'project' && projectId) {
      fetchItems(undefined, projectId);
    } else if (view === 'tag' && tagId) {
      fetchItems(undefined, undefined, tagId);
    } else if (view !== 'project' && view !== 'search' && view !== 'tag') {
      fetchItems(view as TodoViewFilter);
    }
  }

  async function selectItem(id: null | string) {
    selectedItemId.value = id;
    selectedItemDetail.value = null;
    if (id) await fetchItemDetail(id);
  }

  function closeDetail() {
    selectedItemId.value = null;
    selectedItemDetail.value = null;
  }

  // ── Init ───────────────────────────────────────────

  async function init() {
    await Promise.all([fetchProjects(), fetchCounts(), fetchTags(), fetchCategories()]);
    await fetchItems('today');
    currentView.value = 'today';
  }

  return {
    // State
    categories,
    counts,
    currentProjectId,
    currentTagId,
    currentView,
    items,
    loading,
    projects,
    searchQuery,
    searchResults,
    selectedItemDetail,
    selectedItemId,
    tags,
    trashItems,
    // Getters
    completedItems,
    isDetailOpen,
    pendingItems,
    selectedItem,
    todayProgress,
    // Project actions
    createProject,
    deleteProject,
    fetchProjects,
    updateProject,
    // Item actions
    archiveItem,
    batchUpdate,
    createItem,
    deleteItem,
    fetchCounts,
    fetchItemDetail,
    fetchItems,
    fetchTrash,
    permanentDeleteItem,
    restoreItem,
    toggleComplete,
    updateItem,
    // Checklist actions
    addChecklistItem,
    deleteChecklistItem,
    toggleChecklistItem,
    // Tags actions
    createTag,
    deleteTag,
    fetchTags,
    // Categories actions
    fetchCategories,
    // Search actions
    search,
    // UI state
    closeDetail,
    init,
    selectItem,
    setView,
  };
});
