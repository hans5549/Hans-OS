import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

/** 部門分享連結資訊（管理端） */
export interface DepartmentShareInfoResponse {
  id: string;
  token: string;
  permission: string;
  isActive: boolean;
  createdAt: string;
}

/** 年度預算摘要 */
export interface BudgetYearSummary {
  year: number;
  status: string;
}

/** 部門分享總覽（公開端） */
export interface DepartmentShareOverviewResponse {
  departmentName: string;
  permission: string;
  availableYears: BudgetYearSummary[];
}

/** 公開預算項目 */
export interface PublicBudgetItemResponse {
  id: string;
  sequence: number;
  activityName: string;
  contentItem: string;
  amount: number;
  note: string | null;
}

/** 公開預算資料 */
export interface PublicBudgetResponse {
  departmentName: string;
  year: number;
  effectivePermission: string;
  budgetStatus: string;
  items: PublicBudgetItemResponse[];
}

/** 更新分享設定 */
export interface UpdateShareRequest {
  permission?: string;
  isActive?: boolean;
}

/** 公開端預算項目輸入 */
export interface PublicBudgetItemInput {
  id?: string;
  sequence: number;
  activityName: string;
  contentItem: string;
  amount: number;
  note?: string;
}

/** 公開端批次儲存 */
export interface PublicSaveBudgetItemsRequest {
  items: PublicBudgetItemInput[];
}

// ── 管理端 API ────────────────────────────────────

/** 取得或自動建立部門分享 Token */
export const getOrCreateShareApi = (departmentId: string) =>
  requestClient.get<DepartmentShareInfoResponse>(
    `/annual-budgets/departments/${departmentId}/share`,
  );

/** 更新分享設定 */
export const updateShareApi = (
  departmentId: string,
  data: UpdateShareRequest,
) =>
  requestClient.put<DepartmentShareInfoResponse>(
    `/annual-budgets/departments/${departmentId}/share`,
    data,
  );

/** 重新產生 Token */
export const regenerateShareApi = (departmentId: string) =>
  requestClient.post<DepartmentShareInfoResponse>(
    `/annual-budgets/departments/${departmentId}/share/regenerate`,
  );

/** 撤銷分享 Token */
export const revokeShareApi = (departmentId: string) =>
  requestClient.delete(
    `/annual-budgets/departments/${departmentId}/share`,
  );

// ── 公開端 API ────────────────────────────────────

/** 透過 Token 取得部門總覽（部門名稱 + 可用年度） */
export const getPublicDepartmentOverviewApi = (token: string) =>
  requestClient.get<DepartmentShareOverviewResponse>(
    `/public/department-budget/${token}`,
  );

/** 透過 Token 取得特定年度預算資料 */
export const getPublicDepartmentBudgetApi = (token: string, year: number) =>
  requestClient.get<PublicBudgetResponse>(
    `/public/department-budget/${token}/years/${year}`,
  );

/** 透過 Token 儲存特定年度預算項目 */
export const savePublicDepartmentBudgetItemsApi = (
  token: string,
  year: number,
  data: PublicSaveBudgetItemsRequest,
) =>
  requestClient.put<PublicBudgetItemResponse[]>(
    `/public/department-budget/${token}/years/${year}/items`,
    data,
  );
