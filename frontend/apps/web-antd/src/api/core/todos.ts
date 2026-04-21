import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export type TodoPriority = 'None' | 'Low' | 'Medium' | 'High';
export type TodoStatus = 'Pending' | 'InProgress' | 'Done';
export type TodoViewFilter = 'inbox' | 'today' | 'upcoming' | 'all';

export interface TodoProject {
  id: string;
  name: string;
  color: string;
  order: number;
  isArchived: boolean;
  itemCount: number;
}

export interface TodoItem {
  id: string;
  title: string;
  description: string | null;
  priority: TodoPriority;
  status: TodoStatus;
  dueDate: string | null;
  projectId: string | null;
  projectName: string | null;
  projectColor: string | null;
  order: number;
  createdAt: string;
  completedAt: string | null;
}

export interface TodoCounts {
  inbox: number;
  today: number;
  upcoming: number;
  all: number;
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
  description?: string;
  priority?: TodoPriority;
  dueDate?: string | null;
  projectId?: string | null;
}

export interface UpdateItemRequest {
  title: string;
  description?: string | null;
  priority?: TodoPriority;
  status?: TodoStatus;
  dueDate?: string | null;
  projectId?: string | null;
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

/** 取得任務列表（依視圖或專案篩選） */
export const getItemsApi = (params: {
  view?: TodoViewFilter;
  projectId?: string;
}) => requestClient.get<TodoItem[]>('/todo/items', { params });

/** 取得各視圖待辦數量 */
export const getCountsApi = () =>
  requestClient.get<TodoCounts>('/todo/counts');

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

/** 刪除任務 */
export const deleteItemApi = (id: string) =>
  requestClient.delete(`/todo/items/${id}`);
