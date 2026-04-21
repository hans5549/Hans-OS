import { requestClient } from '#/api/request';

// ── 型別定義 ──────────────────────────────────────

export type FinanceTaskPriority = 0 | 1 | 2; // 0=High, 1=Medium, 2=Low
export type FinanceTaskStatus = 0 | 1 | 2; // 0=Pending, 1=InProgress, 2=Completed
export type UnifiedTaskType = 0 | 1 | 2; // 0=General, 1=Remittance, 2=Receipt

export interface FinanceTaskResponse {
  id: string;
  title: string;
  description: string | null;
  priority: FinanceTaskPriority;
  status: FinanceTaskStatus;
  dueDate: string | null;
  departmentId: string | null;
  departmentName: string | null;
  completedAt: string | null;
  createdAt: string;
}

export interface CreateFinanceTaskRequest {
  title: string;
  description?: string;
  priority: FinanceTaskPriority;
  dueDate?: string;
  departmentId?: string;
}

export interface UpdateFinanceTaskRequest {
  title: string;
  description?: string;
  priority: FinanceTaskPriority;
  dueDate?: string;
  departmentId?: string;
}

export interface UnifiedTaskItem {
  id: string;
  title: string;
  description: string | null;
  type: UnifiedTaskType;
  priority: FinanceTaskPriority;
  status: FinanceTaskStatus;
  dueDate: string | null;
  departmentName: string | null;
  createdAt: string;
  sourceId: string | null;
}

export interface UnifiedTaskListResponse {
  tasks: UnifiedTaskItem[];
  totalCount: number;
  pendingCount: number;
  inProgressCount: number;
  completedCount: number;
}

// ── API 函式 ──────────────────────────────────────

/** 取得統一任務清單（聚合一般任務、待匯款、收據） */
export async function getUnifiedTasksApi(params?: {
  month?: number;
  status?: FinanceTaskStatus;
  type?: UnifiedTaskType;
  year?: number;
}) {
  const searchParams = new URLSearchParams();
  if (params?.year !== undefined)
    searchParams.set('year', String(params.year));
  if (params?.month !== undefined)
    searchParams.set('month', String(params.month));
  if (params?.status !== undefined)
    searchParams.set('status', String(params.status));
  if (params?.type !== undefined)
    searchParams.set('type', String(params.type));
  const query = searchParams.toString();
  return requestClient.get<UnifiedTaskListResponse>(
    `/finance-tasks/unified${query ? `?${query}` : ''}`,
  );
}

/** 取得一般財務任務列表 */
export async function getFinanceTasksApi(status?: FinanceTaskStatus) {
  const params = new URLSearchParams();
  if (status !== undefined) params.set('status', String(status));
  const query = params.toString();
  return requestClient.get<FinanceTaskResponse[]>(
    `/finance-tasks${query ? `?${query}` : ''}`,
  );
}

/** 取得單筆財務任務 */
export async function getFinanceTaskByIdApi(id: string) {
  return requestClient.get<FinanceTaskResponse>(`/finance-tasks/${id}`);
}

/** 新增財務任務 */
export async function createFinanceTaskApi(data: CreateFinanceTaskRequest) {
  return requestClient.post<FinanceTaskResponse>('/finance-tasks', data);
}

/** 更新財務任務 */
export async function updateFinanceTaskApi(
  id: string,
  data: UpdateFinanceTaskRequest,
) {
  return requestClient.put(`/finance-tasks/${id}`, data);
}

/** 刪除財務任務 */
export async function deleteFinanceTaskApi(id: string) {
  return requestClient.delete(`/finance-tasks/${id}`);
}

/** 標記任務完成 */
export async function completeFinanceTaskApi(id: string) {
  return requestClient.put(`/finance-tasks/${id}/complete`);
}
