import { requestClient } from '#/api/request';

// ── 型別定義 ──────────────────────────────────────

export type PendingRemittanceStatus = 0 | 1; // 0=Pending, 1=Completed

export interface PendingRemittanceResponse {
  id: string;
  description: string;
  amount: number;
  sourceAccount: string;
  targetAccount: string;
  departmentId: string | null;
  departmentName: string | null;
  recipientName: string | null;
  expectedDate: string | null;
  note: string | null;
  status: PendingRemittanceStatus;
  completedAt: string | null;
  createdAt: string;
  activityExpenseId: string | null;
  activityExpenseDescription: string | null;
}

export interface CreatePendingRemittanceRequest {
  description: string;
  amount: number;
  sourceAccount: string;
  targetAccount: string;
  departmentId?: string;
  recipientName?: string;
  expectedDate?: string;
  note?: string;
  activityExpenseId?: string;
}

export interface UpdatePendingRemittanceRequest {
  description: string;
  amount: number;
  sourceAccount: string;
  targetAccount: string;
  departmentId?: string;
  recipientName?: string;
  expectedDate?: string;
  note?: string;
  activityExpenseId?: string;
}

export interface CompletePendingRemittanceRequest {
  bankName: string;
  transactionDate: string;
}

// ── API 函式 ──────────────────────────────────────

/** 取得待匯款列表（可依狀態篩選） */
export async function getPendingRemittancesApi(
  status?: PendingRemittanceStatus,
) {
  const params = new URLSearchParams();
  if (status !== undefined) {
    params.set('status', String(status));
  }
  const query = params.toString();
  return requestClient.get<PendingRemittanceResponse[]>(
    `/pending-remittances${query ? `?${query}` : ''}`,
  );
}

/** 取得單筆待匯款 */
export async function getPendingRemittanceByIdApi(id: string) {
  return requestClient.get<PendingRemittanceResponse>(
    `/pending-remittances/${id}`,
  );
}

/** 新增待匯款 */
export async function createPendingRemittanceApi(
  data: CreatePendingRemittanceRequest,
) {
  return requestClient.post<PendingRemittanceResponse>(
    '/pending-remittances',
    data,
  );
}

/** 更新待匯款 */
export async function updatePendingRemittanceApi(
  id: string,
  data: UpdatePendingRemittanceRequest,
) {
  return requestClient.put(`/pending-remittances/${id}`, data);
}

/** 刪除待匯款 */
export async function deletePendingRemittanceApi(id: string) {
  return requestClient.delete(`/pending-remittances/${id}`);
}

/** 標記匯款完成（自動建立收支表支出） */
export async function completePendingRemittanceApi(
  id: string,
  data: CompletePendingRemittanceRequest,
) {
  return requestClient.put(`/pending-remittances/${id}/complete`, data);
}
