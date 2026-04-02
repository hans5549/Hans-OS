import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

export interface AnnualBudgetOverviewResponse {
  id: string;
  year: number;
  status: string;
  note: string | null;
  totalBudget: number;
  totalActual: number;
  grantedBudget: number | null;
  departments: DepartmentBudgetSummaryResponse[];
}

export interface DepartmentBudgetSummaryResponse {
  departmentBudgetId: string;
  departmentId: string;
  departmentName: string;
  budgetAmount: number;
  actualAmount: number;
  allocatedAmount: number | null;
  itemCount: number;
}

export interface BudgetItemResponse {
  id: string;
  sequence: number;
  activityName: string;
  contentItem: string;
  amount: number;
  note: string | null;
  actualAmount: number;
}

export interface BudgetItemInput {
  id?: string;
  sequence: number;
  activityName: string;
  contentItem: string;
  amount: number;
  note?: string;
}

export interface SaveBudgetItemsRequest {
  items: BudgetItemInput[];
}

export interface UpdateBudgetStatusRequest {
  status: string;
}

export interface UpdateGrantedBudgetRequest {
  grantedBudget: number;
}

// ── API ───────────────────────────────────────────

/** 取得年度預算總覽（自動初始化） */
export const getAnnualBudgetOverviewApi = (year: number) =>
  requestClient.get<AnnualBudgetOverviewResponse>(`/annual-budgets/${year}`);

/** 更新預算狀態 */
export const updateBudgetStatusApi = (
  year: number,
  data: UpdateBudgetStatusRequest,
) => requestClient.put(`/annual-budgets/${year}/status`, data);

/** 取得部門預算項目 */
export const getDepartmentBudgetItemsApi = (
  year: number,
  departmentId: string,
) =>
  requestClient.get<BudgetItemResponse[]>(
    `/annual-budgets/${year}/departments/${departmentId}/items`,
  );

/** 批次儲存部門預算項目 */
export const saveDepartmentBudgetItemsApi = (
  year: number,
  departmentId: string,
  data: SaveBudgetItemsRequest,
) =>
  requestClient.put<BudgetItemResponse[]>(
    `/annual-budgets/${year}/departments/${departmentId}/items`,
    data,
  );

/** 更新核定總預算 */
export const updateGrantedBudgetApi = (
  year: number,
  data: UpdateGrantedBudgetRequest,
) =>
  requestClient.put<AnnualBudgetOverviewResponse>(
    `/annual-budgets/${year}/granted-budget`,
    data,
  );
