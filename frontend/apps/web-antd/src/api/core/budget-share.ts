import { requestClient } from '#/api/request';

// ── Types ─────────────────────────────────────────

/** 分享連結資訊（管理端） */
export interface BudgetShareInfoResponse {
  id: string;
  token: string;
  permission: string;
  isActive: boolean;
  effectivePermission: string;
  createdAt: string;
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

/** 建立或重新產生分享 Token */
export const createBudgetShareApi = (year: number, departmentId: string) =>
  requestClient.post<BudgetShareInfoResponse>(
    `/annual-budgets/${year}/departments/${departmentId}/share`,
  );

/** 取得分享資訊 */
export const getBudgetShareApi = (year: number, departmentId: string) =>
  requestClient.get<BudgetShareInfoResponse | null>(
    `/annual-budgets/${year}/departments/${departmentId}/share`,
  );

/** 更新分享設定 */
export const updateBudgetShareApi = (
  year: number,
  departmentId: string,
  data: UpdateShareRequest,
) =>
  requestClient.put<BudgetShareInfoResponse>(
    `/annual-budgets/${year}/departments/${departmentId}/share`,
    data,
  );

/** 撤銷分享 Token */
export const revokeBudgetShareApi = (year: number, departmentId: string) =>
  requestClient.delete(
    `/annual-budgets/${year}/departments/${departmentId}/share`,
  );

// ── 公開端 API ────────────────────────────────────

/** 透過 Token 取得預算資料 */
export const getPublicBudgetApi = (token: string) =>
  requestClient.get<PublicBudgetResponse>(`/public/budget/${token}`);

/** 透過 Token 儲存預算項目 */
export const savePublicBudgetItemsApi = (
  token: string,
  data: PublicSaveBudgetItemsRequest,
) =>
  requestClient.put<PublicBudgetItemResponse[]>(
    `/public/budget/${token}/items`,
    data,
  );
