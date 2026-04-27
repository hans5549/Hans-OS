import { computed, ref } from 'vue';

import { defineStore } from 'pinia';

import type {
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
  archiveItemApi,
  batchUpdateApi,
  createItemApi,
  createProjectApi,
  createTagApi,
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
  reorderChildrenApi,
  restoreItemApi,
  searchItemsApi,
  toggleCompleteApi,
  updateItemApi,
  updateProjectApi,
} from '#/api/core/todos';

export type TodoView = 'all' | 'project' | 'search' | 'tag' | 'trash' | 'week' | TodoViewFilter;

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
      const res = await getItemsApi({
        includeChildren: true,
        projectId,
        tagId,
        topLevelOnly: true,
        view,
      });
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
      replaceItemEverywhere(updated);
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
    const original = findItemEverywhere(id);
    if (!original) return;

    const isCurrentlyDone = original.status === 'Done';
    replaceItemEverywhere({
      ...original,
      completedAt: isCurrentlyDone ? null : new Date().toISOString(),
      status: isCurrentlyDone ? 'Pending' : 'Done',
    });

    try {
      const updated = await toggleCompleteApi(id);
      replaceItemEverywhere(updated);
      if (selectedItemDetail.value?.id === id) await fetchItemDetail(id);
      await fetchCounts();
    } catch {
      replaceItemEverywhere(original);
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
    removeChildEverywhere(id);
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

  function findItemEverywhere(id: string) {
    for (const item of items.value) {
      if (item.id === id) return item;
      const child = item.children.find((c: TodoItem) => c.id === id);
      if (child) return child;
    }
    return selectedItemDetail.value?.children.find((child: TodoItem) => child.id === id) ?? null;
  }

  function replaceItemEverywhere(updated: TodoItem) {
    items.value = items.value.map((item: TodoItem) => {
      if (item.id === updated.id) return { ...updated, children: item.children };
      return {
        ...item,
        children: item.children.map((child: TodoItem) =>
          child.id === updated.id ? { ...updated, children: child.children } : child,
        ),
      };
    });

    if (selectedItemDetail.value?.id === updated.id) return;
    if (selectedItemDetail.value) {
      selectedItemDetail.value = {
        ...selectedItemDetail.value,
        children: selectedItemDetail.value.children.map((child: TodoItem) =>
          child.id === updated.id ? { ...updated, children: child.children } : child,
        ),
      };
    }
  }

  function appendChildEverywhere(parentId: string, child: TodoItem) {
    replaceChildrenEverywhere(parentId, [...findParentChildren(parentId), child]);
  }

  function removeChildEverywhere(childId: string) {
    items.value = items.value.map((item: TodoItem) => ({
      ...item,
      children: item.children.filter((child: TodoItem) => child.id !== childId),
    }));

    if (selectedItemDetail.value) {
      selectedItemDetail.value = {
        ...selectedItemDetail.value,
        children: selectedItemDetail.value.children.filter((child: TodoItem) => child.id !== childId),
      };
    }
  }

  function findParentChildren(parentId: string) {
    const parent = items.value.find((item: TodoItem) => item.id === parentId);
    if (parent) return parent.children;
    if (selectedItemDetail.value?.id === parentId) return selectedItemDetail.value.children;
    return [];
  }

  function replaceChildrenEverywhere(parentId: string, children: TodoItem[]) {
    items.value = items.value.map((item: TodoItem) =>
      item.id === parentId ? { ...item, children } : item,
    );

    if (selectedItemDetail.value?.id === parentId) {
      selectedItemDetail.value = { ...selectedItemDetail.value, children };
    }
  }

  // ── Child item actions ─────────────────────────────

  async function createChildItem(parentId: string, data: Omit<CreateItemRequest, 'parentId'>) {
    loading.value.saving = true;
    try {
      const child = await createItemApi({ ...data, parentId });
      appendChildEverywhere(parentId, child);
      await fetchCounts();
      return child;
    } finally {
      loading.value.saving = false;
    }
  }

  async function reorderChildren(parentId: string, orderedChildIds: string[]) {
    const original = findParentChildren(parentId);
    const nextChildren = [...original].sort(
      (a, b) => orderedChildIds.indexOf(a.id) - orderedChildIds.indexOf(b.id),
    );
    replaceChildrenEverywhere(parentId, nextChildren);

    try {
      const updated = await reorderChildrenApi(parentId, orderedChildIds);
      replaceChildrenEverywhere(parentId, updated);
    } catch (error) {
      replaceChildrenEverywhere(parentId, original);
      throw error;
    }
  }

  function reorderChildBefore(parentId: string, draggedId: string, targetId: string) {
    if (draggedId === targetId) return;
    const children = [...findParentChildren(parentId)];
    const from = children.findIndex((child: TodoItem) => child.id === draggedId);
    const to = children.findIndex((child: TodoItem) => child.id === targetId);
    if (from < 0 || to < 0) return;

    const [moved] = children.splice(from, 1);
    if (!moved) return;

    children.splice(to, 0, moved);
    reorderChildren(parentId, children.map((child: TodoItem) => child.id));
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
    createChildItem,
    deleteItem,
    fetchCounts,
    fetchItemDetail,
    fetchItems,
    fetchTrash,
    permanentDeleteItem,
    reorderChildren,
    reorderChildBefore,
    restoreItem,
    toggleComplete,
    updateItem,
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
