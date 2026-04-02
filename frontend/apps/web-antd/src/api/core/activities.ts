import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface ActivitySummaryResponse {
  id: string;
  name: string;
  description: string | null;
  departmentId: string;
  departmentName: string;
  year: number;
  month: number;
  sequence: number;
  totalAmount: number;
  groupCount: number;
  expenseCount: number;
}

export interface ActivityDetailResponse {
  id: string;
  name: string;
  description: string | null;
  departmentId: string;
  departmentName: string;
  year: number;
  month: number;
  sequence: number;
  totalAmount: number;
  groups: ActivityGroupResponse[];
  ungroupedExpenses: ActivityExpenseResponse[];
}

export interface ActivityGroupResponse {
  id: string;
  name: string;
  sequence: number;
  subTotal: number;
  expenses: ActivityExpenseResponse[];
}

export interface ActivityExpenseResponse {
  id: string;
  description: string;
  amount: number;
  note: string | null;
  sequence: number;
  budgetItemId: string | null;
  budgetItemName: string | null;
}

export interface MonthSummaryResponse {
  month: number;
  activityCount: number;
  totalAmount: number;
}

export interface CreateActivityRequest {
  departmentId: string;
  year: number;
  month: number;
  name: string;
  description?: string;
  groups?: ActivityGroupInput[];
  expenses?: ActivityExpenseInput[];
}

export interface UpdateActivityRequest {
  name: string;
  description?: string;
  month?: number;
  groups?: ActivityGroupInput[];
  expenses?: ActivityExpenseInput[];
}

export interface ActivityGroupInput {
  id?: string;
  name: string;
  sequence: number;
  expenses: ActivityExpenseInput[];
}

export interface ActivityExpenseInput {
  id?: string;
  description: string;
  amount: number;
  note?: string;
  sequence: number;
  budgetItemId?: string;
}

// ── API ───────────────────────────────────────────

/** 取得活動列表 */
export const getActivitiesApi = (
  year: number,
  month?: number,
  departmentId?: string,
) =>
  requestClient.get<ActivitySummaryResponse[]>('/activities', {
    params: { year, month, departmentId },
  });

/** 取得各月活動統計 */
export const getActivityMonthSummariesApi = (
  year: number,
  departmentId?: string,
) =>
  requestClient.get<MonthSummaryResponse[]>('/activities/month-summaries', {
    params: { year, departmentId },
  });

/** 取得活動明細 */
export const getActivityDetailApi = (id: string) =>
  requestClient.get<ActivityDetailResponse>(`/activities/${id}`);

/** 新增活動 */
export const createActivityApi = (data: CreateActivityRequest) =>
  requestClient.post<ActivityDetailResponse>('/activities', data);

/** 更新活動 */
export const updateActivityApi = (id: string, data: UpdateActivityRequest) =>
  requestClient.put<ActivityDetailResponse>(`/activities/${id}`, data);

/** 刪除活動 */
export const deleteActivityApi = (id: string) =>
  requestClient.delete(`/activities/${id}`);
