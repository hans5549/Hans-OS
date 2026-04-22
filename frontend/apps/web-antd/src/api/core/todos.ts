import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export type TodoPriority = 'High' | 'Low' | 'Medium' | 'None' | 'Urgent';
export type TodoStatus = 'Done' | 'InProgress' | 'Pending';
export type TodoDifficulty = 'Easy' | 'Hard' | 'Medium' | 'None';
export type TodoRecurrencePattern = 'Daily' | 'Monthly' | 'None' | 'Weekly' | 'Yearly';
export type TodoViewFilter = 'all' | 'inbox' | 'today' | 'upcoming';

export interface TodoProject {
  id: string;
  name: string;
  color: string;
  order: number;
  isArchived: boolean;
  itemCount: number;
}

export interface TodoTag {
  id: string;
  name: string;
  color: string | null;
}

export interface TodoCategory {
  id: string;
  name: string;
  color: string | null;
  icon: string | null;
  sortOrder: number;
}

export interface ChecklistItem {
  id: string;
  title: string;
  isCompleted: boolean;
  order: number;
}

export interface TodoItem {
  id: string;
  title: string;
  description: string | null;
  priority: TodoPriority;
  status: TodoStatus;
  difficulty: TodoDifficulty;
  dueDate: string | null;
  scheduledDate: string | null;
  projectId: string | null;
  projectName: string | null;
  projectColor: string | null;
  parentId: string | null;
  order: number;
  createdAt: string;
  completedAt: string | null;
  archivedAt: string | null;
  tags: TodoTag[];
}

export interface TodoItemDetail extends TodoItem {
  categoryId: string | null;
  categoryName: string | null;
  recurrencePattern: TodoRecurrencePattern;
  recurrenceInterval: number;
  archivedAt: string | null;
  children: TodoItem[];
  checklistItems: ChecklistItem[];
}

export interface PagedItemsResponse {
  items: TodoItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TodoCounts {
  inbox: number;
  today: number;
  upcoming: number;
  all: number;
}

export interface TodoStats {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
  overdue: number;
  archived: number;
}

export interface CreateProjectRequest {
  name: string;
  color?: string;
}

export interface UpdateProjectRequest {
  name: string;
  color: string;
  isArchived?: boolean;
}

export interface CreateItemRequest {
  title: string;
  description?: string | null;
  priority?: TodoPriority;
  difficulty?: TodoDifficulty;
  dueDate?: string | null;
  scheduledDate?: string | null;
  projectId?: string | null;
  parentId?: string | null;
  categoryId?: string | null;
  tagIds?: string[];
  recurrencePattern?: TodoRecurrencePattern;
  recurrenceInterval?: number;
}

export interface UpdateItemRequest {
  title: string;
  description?: string | null;
  priority?: TodoPriority;
  difficulty?: TodoDifficulty;
  status?: TodoStatus;
  dueDate?: string | null;
  scheduledDate?: string | null;
  projectId?: string | null;
  parentId?: string | null;
  categoryId?: string | null;
  tagIds?: string[];
  recurrencePattern?: TodoRecurrencePattern;
  recurrenceInterval?: number;
}

export interface CreateTagRequest {
  name: string;
  color?: string | null;
}

export interface UpdateTagRequest {
  name: string;
  color?: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  color?: string | null;
  icon?: string | null;
  sortOrder?: number;
}

export interface UpdateCategoryRequest {
  name: string;
  color?: string | null;
  icon?: string | null;
  sortOrder?: number;
}

export interface CreateChecklistItemRequest {
  title: string;
  order?: number;
}

export interface UpdateChecklistItemRequest {
  title?: string | null;
  isCompleted?: boolean | null;
  order?: number | null;
}

// ── Project API ───────────────────────────────────

/** 取得所有專案 */
export const getProjectsApi = () =>
  requestClient.get<TodoProject[]>('/todo/projects');

/** 建立專案 */
export const createProjectApi = (data: CreateProjectRequest) =>
  requestClient.post<TodoProject>('/todo/projects', data);

/** 更新專案 */
export const updateProjectApi = (id: string, data: UpdateProjectRequest) =>
  requestClient.put<TodoProject>(`/todo/projects/${id}`, data);

/** 刪除專案 */
export const deleteProjectApi = (id: string) =>
  requestClient.delete(`/todo/projects/${id}`);

// ── Item API ──────────────────────────────────────

/** 取得分頁任務列表（依視圖或專案篩選） */
export const getItemsApi = (params: {
  page?: number;
  pageSize?: number;
  priority?: TodoPriority;
  projectId?: string;
  search?: string;
  status?: TodoStatus;
  tagId?: string;
  view?: TodoViewFilter;
}) => requestClient.get<PagedItemsResponse>('/todo/items', { params });

/** 取得任務詳情（含 checklist、子任務、標籤） */
export const getItemDetailApi = (id: string) =>
  requestClient.get<TodoItemDetail>(`/todo/items/${id}`);

/** 取得各視圖待辦數量 */
export const getCountsApi = () =>
  requestClient.get<TodoCounts>('/todo/counts');

/** 取得統計資訊 */
export const getStatsApi = () =>
  requestClient.get<TodoStats>('/todo/items/stats');

/** 取得垃圾桶任務 */
export const getTrashApi = () =>
  requestClient.get<TodoItem[]>('/todo/items/trash');

/** 搜尋任務 */
export const searchItemsApi = (q: string) =>
  requestClient.get<TodoItem[]>('/todo/items/search', { params: { q } });

/** 建立任務 */
export const createItemApi = (data: CreateItemRequest) =>
  requestClient.post<TodoItem>('/todo/items', data);

/** 更新任務 */
export const updateItemApi = (id: string, data: UpdateItemRequest) =>
  requestClient.put<TodoItem>(`/todo/items/${id}`, data);

/** 切換完成狀態 */
export const toggleCompleteApi = (id: string) =>
  requestClient.request<TodoItem>(`/todo/items/${id}/complete`, {
    method: 'PATCH',
  });

/** 封存或取消封存任務 */
export const archiveItemApi = (id: string, archive: boolean) =>
  requestClient.put<TodoItemDetail>(`/todo/items/${id}/archive`, { archive });

/** 從垃圾桶還原任務 */
export const restoreItemApi = (id: string) =>
  requestClient.put<object>(`/todo/items/${id}/restore`, {});

/** 永久刪除任務 */
export const permanentDeleteItemApi = (id: string) =>
  requestClient.delete(`/todo/items/${id}/permanent`);

/** 批次更新狀態 */
export const batchUpdateApi = (ids: string[], status: TodoStatus) =>
  requestClient.put<object>('/todo/items/batch', { ids, status });

/** 重新排序 */
export const sortItemsApi = (orderedIds: string[]) =>
  requestClient.put<object>('/todo/items/sort', { orderedIds });

/** 軟刪除任務 */
export const deleteItemApi = (id: string) =>
  requestClient.delete(`/todo/items/${id}`);

// ── Checklist API ─────────────────────────────────

/** 新增清單子項目 */
export const addChecklistItemApi = (itemId: string, data: CreateChecklistItemRequest) =>
  requestClient.post<ChecklistItem>(`/todo/items/${itemId}/checklist`, data);

/** 更新清單子項目 */
export const updateChecklistItemApi = (itemId: string, checklistId: string, data: UpdateChecklistItemRequest) =>
  requestClient.put<ChecklistItem>(`/todo/items/${itemId}/checklist/${checklistId}`, data);

/** 刪除清單子項目 */
export const deleteChecklistItemApi = (itemId: string, checklistId: string) =>
  requestClient.delete(`/todo/items/${itemId}/checklist/${checklistId}`);

// ── Tags API ──────────────────────────────────────

/** 取得所有標籤 */
export const getTagsApi = () =>
  requestClient.get<TodoTag[]>('/todo/tags');

/** 建立標籤 */
export const createTagApi = (data: CreateTagRequest) =>
  requestClient.post<TodoTag>('/todo/tags', data);

/** 更新標籤 */
export const updateTagApi = (id: string, data: UpdateTagRequest) =>
  requestClient.put<TodoTag>(`/todo/tags/${id}`, data);

/** 刪除標籤 */
export const deleteTagApi = (id: string) =>
  requestClient.delete(`/todo/tags/${id}`);

// ── Categories API ────────────────────────────────

/** 取得所有分類 */
export const getCategoriesApi = () =>
  requestClient.get<TodoCategory[]>('/todo/categories');

/** 建立分類 */
export const createCategoryApi = (data: CreateCategoryRequest) =>
  requestClient.post<TodoCategory>('/todo/categories', data);

/** 更新分類 */
export const updateCategoryApi = (id: string, data: UpdateCategoryRequest) =>
  requestClient.put<TodoCategory>(`/todo/categories/${id}`, data);

/** 刪除分類 */
export const deleteCategoryApi = (id: string) =>
  requestClient.delete(`/todo/categories/${id}`);
